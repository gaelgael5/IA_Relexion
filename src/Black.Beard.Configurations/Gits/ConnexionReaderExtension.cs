﻿// NOSONAR
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;


namespace Bb.Converters
{

    /// <summary>
    /// Retrieves the key-value pairs from the given payload.
    /// </summary>
    /// <example>
    /// <code lang="C#">
    /// string payload = "key1=value1;key2=value2";
    /// var keyValues = payload.GetKeyValues();
    /// foreach (var kvp in keyValues)
    /// {
    ///     Console.WriteLine($"{kvp.Key}: {kvp.Value}");
    /// }
    /// </code>
    /// </example>
    internal static class ConnexionReaderExtension
    {

        /// <summary>
        /// Retrieves the key-value pairs from the given payload.
        /// </summary>
        /// <param name="payload">The payload string. Must not be null or empty.</param>
        /// <returns>
        /// A dictionary containing the key-value pairs extracted from the payload.
        /// </returns>
        /// <remarks>
        /// This method parses the payload string and extracts the key-value pairs. The payload string should be in a specific format where each key-value pair is separated by a delimiter.
        /// The method uses the <see cref="DbConnectionOptions"/> class to parse the payload string and retrieve the key-value pairs.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the payload is null or empty.</exception>
        /// <example>
        /// <code lang="C#">
        /// string payload = "key1=value1;key2=value2";
        /// var keyValues = payload.GetKeyValues();
        /// foreach (var kvp in keyValues)
        /// {
        ///     Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        /// }
        /// </code>
        /// </example>
        public static Dictionary<string, string> GetKeyValues(this string payload)
        {
            if (string.IsNullOrEmpty(payload))
                throw new ArgumentNullException(nameof(payload));

            Dictionary<string, string> e = new Dictionary<string, string>();
            DbConnectionOptions constr = new DbConnectionOptions(payload, null, false);

            for (NameValuePair? pair = constr._keyChain; null != pair; pair = pair.Next)
                e[pair.Name] = pair.Value;

            return e;
        }

    }

    internal sealed partial class DbConnectionOptions
    {
        // instances of this class are intended to be immutable, i.e readonly
        // used by pooling classes so it is easier to verify correctness
        // without the risk of the objects of the class being modified during execution.


        // differences between OleDb and Odbc
        // ODBC:
        //     https://learn.microsoft.com/sql/odbc/reference/syntax/sqldriverconnect-function
        //     do not support == -> = in keywords
        //     first key-value pair wins
        //     quote values using \{ and \}, only driver= and pwd= appear to generically allow quoting
        //     do not strip quotes from value, or add quotes except for driver keyword
        // OLEDB:
        //     https://learn.microsoft.com/dotnet/framework/data/adonet/connection-string-syntax#oledb-connection-string-syntax
        //     support == -> = in keywords
        //     last key-value pair wins
        //     quote values using \" or \'
        //     strip quotes from value
        internal readonly bool _useOdbcRules;
        internal readonly bool _hasUserIdKeyword;

        // synonyms hashtable is meant to be read-only translation of parsed string
        // keywords/synonyms to a known keyword string
        public DbConnectionOptions(string? connectionString, Dictionary<string, string>? synonyms, bool useOdbcRules)
        {
            _useOdbcRules = useOdbcRules;
            _parsetable = new Dictionary<string, string?>();
            _usersConnectionString = connectionString ?? "";

            // first pass on parsing, initial syntax check
            if (0 < _usersConnectionString.Length)
            {
                _keyChain = ParseInternal(_parsetable, _usersConnectionString, true, synonyms, _useOdbcRules);
                 _hasPasswordKeyword = (_parsetable.ContainsKey(KEY.Password) || _parsetable.ContainsKey(SYNONYM.Pwd));
                _hasUserIdKeyword = (_parsetable.ContainsKey(KEY.User_ID) || _parsetable.ContainsKey(SYNONYM.UID));
            }
        }

        internal Dictionary<string, string?> Parsetable => _parsetable;

        public string? this[string keyword] => _parsetable[keyword];

        internal static void AppendKeyValuePairBuilder(StringBuilder builder, string keyName, string? keyValue, bool useOdbcRules)
        {
            ADP.CheckArgumentNull(builder, nameof(builder));
            ADP.CheckArgumentLength(keyName, nameof(keyName));

            if ((null == keyName) || !s_connectionStringValidKeyRegex.IsMatch(keyName))
            {
                throw ADP.InvalidKeyname(keyName);
            }
            if ((null != keyValue) && !IsValueValidInternal(keyValue))
            {
                throw ADP.InvalidValue(keyName);
            }

            if ((0 < builder.Length) && (';' != builder[builder.Length - 1]))
            {
                builder.Append(';');
            }

            if (useOdbcRules)
            {
                builder.Append(keyName);
            }
            else
            {
                builder.Append(keyName.Replace("=", "=="));
            }
            builder.Append('=');

            if (null != keyValue)
            {
                // else <keyword>=;
                if (useOdbcRules)
                {
                    if ((0 < keyValue.Length) &&
                        // string.Contains(char) is .NetCore2.1+ specific
                        (('{' == keyValue[0]) || (0 <= keyValue.IndexOf(';')) || string.Equals(DbConnectionStringKeywords.Driver, keyName, StringComparison.OrdinalIgnoreCase)) &&
                        !s_connectionStringQuoteOdbcValueRegex.IsMatch(keyValue))
                    {
                        // always quote Driver value (required for ODBC Version 2.65 and earlier)
                        // always quote values that contain a ';'
                        builder.Append('{').Append(keyValue.Replace("}", "}}")).Append('}');
                    }
                    else
                    {
                        builder.Append(keyValue);
                    }
                }
                else if (s_connectionStringQuoteValueRegex.IsMatch(keyValue))
                {
                    // <value> -> <value>
                    builder.Append(keyValue);
                }
                else if ((keyValue.Contains('\"')) && (!keyValue.Contains('\'')))
                {
                    // <val"ue> -> <'val"ue'>
                    builder.Append('\'');
                    builder.Append(keyValue);
                    builder.Append('\'');
                }
                else
                {
                    // <val'ue> -> <"val'ue">
                    // <=value> -> <"=value">
                    // <;value> -> <";value">
                    // < value> -> <" value">
                    // <va lue> -> <"va lue">
                    // <va'"lue> -> <"va'""lue">
                    builder.Append('\"');
                    builder.Append(keyValue.Replace("\"", "\"\""));
                    builder.Append('\"');
                }
            }
        }

        internal string Expand() => _usersConnectionString;

        internal string ExpandKeyword(string keyword, string replacementValue)
        {
            // preserve duplicates, updated keyword value with replacement value
            // if keyword not specified, append to end of the string
            bool expanded = false;
            int copyPosition = 0;

            var builder = new StringBuilder(_usersConnectionString.Length);
            for (NameValuePair? current = _keyChain; null != current; current = current.Next)
            {
                if ((current.Name == keyword) && (current.Value == this[keyword]))
                {
                    // only replace the parse end-result value instead of all values
                    // so that when duplicate-keywords occur other original values remain in place
                    AppendKeyValuePairBuilder(builder, current.Name, replacementValue, _useOdbcRules);
                    builder.Append(';');
                    expanded = true;
                }
                else
                {
                    builder.Append(_usersConnectionString, copyPosition, current.Length);
                }
                copyPosition += current.Length;
            }

            if (!expanded)
            {
                Debug.Assert(!_useOdbcRules, "ExpandKeyword not ready for Odbc");
                AppendKeyValuePairBuilder(builder, keyword, replacementValue, _useOdbcRules);
            }
            return builder.ToString();
        }

        [Conditional("DEBUG")]
        static partial void DebugTraceKeyValuePair(string keyname, string? keyvalue, Dictionary<string, string>? synonyms)
        {
            Debug.Assert(keyname == keyname.ToLowerInvariant(), "missing ToLower");

            string realkeyname = ((null != synonyms) ? (string)synonyms[keyname] : keyname);
            if ((KEY.Password != realkeyname) && (SYNONYM.Pwd != realkeyname))
            {
                // don't trace passwords ever!
                if (null != keyvalue)
                {
                    Trace.TraceWarning(string.Format("<comm.DbConnectionOptions|INFO|ADV> KeyName='{0}', KeyValue='{1}'", keyname, keyvalue));
                }
                else
                {
                    Trace.TraceWarning(string.Format("<comm.DbConnectionOptions|INFO|ADV> KeyName='{0}'", keyname));
                }
            }
        }

        internal static void ValidateKeyValuePair(string keyword, string value)
        {
            if ((null == keyword) || !s_connectionStringValidKeyRegex.IsMatch(keyword))
            {
                throw ADP.InvalidKeyname(keyword);
            }
            if ((null != value) && value.Contains('\0'))
            {
                throw ADP.InvalidValue(keyword);
            }
        }

        internal static class DbConnectionStringKeywords
        {
            internal const string Driver = "Driver";
            internal const string Password = "Password";
        }


    }

    internal sealed class NameValuePair
    {
        private readonly string _name;
        private readonly string? _value;
        private readonly int _length;
        private NameValuePair? _next;

        internal NameValuePair(string name, string? value, int length)
        {
            Debug.Assert(!string.IsNullOrEmpty(name), "empty keyname");
            _name = name;
            _value = value;
            _length = length;
        }

        internal int Length
        {
            get
            {
                Debug.Assert(0 < _length, "NameValuePair zero Length usage");
                return _length;
            }
        }

        internal string Name => _name;
        internal string? Value => _value;

        internal NameValuePair? Next
        {
            get { return _next; }
            set
            {
                if ((null != _next) || (null == value))
                {
                    throw ADP.InternalError();
                }
                _next = value;
            }
        }
    }

    internal partial class DbConnectionOptions
    {
#if DEBUG
        private const string ConnectionStringPattern =                      // may not contain embedded null except trailing last value
                "([\\s;]*"                                                  // leading whitespace and extra semicolons
                + "(?![\\s;])"                                              // key does not start with space or semicolon
                + "(?<key>([^=\\s\\p{Cc}]|\\s+[^=\\s\\p{Cc}]|\\s+==|==)+)"  // allow any visible character for keyname except '=' which must quoted as '=='
                + "\\s*=(?!=)\\s*"                                          // the equal sign divides the key and value parts
                + "(?<value>"
                + "(\"([^\"\u0000]|\"\")*\")"                               // double quoted string, " must be quoted as ""
                + "|"
                + "('([^'\u0000]|'')*')"                                    // single quoted string, ' must be quoted as ''
                + "|"
                + "((?![\"'\\s])"                                           // unquoted value must not start with " or ' or space, would also like = but too late to change
                + "([^;\\s\\p{Cc}]|\\s+[^;\\s\\p{Cc}])*"                    // control characters must be quoted
                + "(?<![\"']))"                                            // unquoted value must not stop with " or '
                + ")(\\s*)(;|[\u0000\\s]*$)"                               // whitespace after value up to semicolon or end-of-line
                + ")*"                                                     // repeat the key-value pair
                + "[\\s;]*[\u0000\\s]*"                                    // trailing whitespace/semicolons (DataSourceLocator), embedded nulls are allowed only in the end
            ;

        private const string ConnectionStringPatternOdbc =                 // may not contain embedded null except trailing last value
                "([\\s;]*"                                                 // leading whitespace and extra semicolons
                + "(?![\\s;])"                                             // key does not start with space or semicolon
                + "(?<key>([^=\\s\\p{Cc}]|\\s+[^=\\s\\p{Cc}])+)"           // allow any visible character for keyname except '='
                + "\\s*=\\s*"                                              // the equal sign divides the key and value parts
                + "(?<value>"
                + "(\\{([^\\}\u0000]|\\}\\})*\\})"                         // quoted string, starts with { and ends with }
                + "|"
                + "((?![\\{\\s])"                                          // unquoted value must not start with { or space, would also like = but too late to change
                + "([^;\\s\\p{Cc}]|\\s+[^;\\s\\p{Cc}])*"                   // control characters must be quoted

                + ")" // although the spec does not allow {} embedded within a value, the retail code does.
                + ")(\\s*)(;|[\u0000\\s]*$)"                               // whitespace after value up to semicolon or end-of-line
                + ")*"                                                     // repeat the key-value pair
                + "[\\s;]*[\u0000\\s]*"                                    // trailing whitespace/semicolons (DataSourceLocator), embedded nulls are allowed only in the end
            ;

        private static readonly Regex s_connectionStringRegex = CreateConnectionStringRegex();
        private static readonly Regex s_connectionStringRegexOdbc = CreateConnectionStringRegexOdbc();

//#if NET
//        [GeneratedRegex(ConnectionStringPattern, RegexOptions.ExplicitCapture)]
//        private static partial Regex CreateConnectionStringRegex();

//        [GeneratedRegex(ConnectionStringPatternOdbc, RegexOptions.ExplicitCapture)]
//        private static partial Regex CreateConnectionStringRegexOdbc();
//#else
        private static Regex CreateConnectionStringRegex() => new Regex(ConnectionStringPattern, RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static Regex CreateConnectionStringRegexOdbc() => new Regex(ConnectionStringPatternOdbc, RegexOptions.ExplicitCapture | RegexOptions.Compiled);
//#endif
#endif
        internal const string DataDirectory = "|datadirectory|";

        private static readonly Regex s_connectionStringValidKeyRegex = CreateConnectionStringValidKeyRegex(); // key not allowed to start with semi-colon or space or contain non-visible characters or end with space
        private static readonly Regex s_connectionStringQuoteValueRegex = CreateConnectionStringQuoteValueRegex(); // generally do not quote the value if it matches the pattern
        private static readonly Regex s_connectionStringQuoteOdbcValueRegex = CreateConnectionStringQuoteOdbcValueRegex(); // do not quote odbc value if it matches this pattern

//#if NET
//        [GeneratedRegex("^(?![;\\s])[^\\p{Cc}]+(?<!\\s)$")]
//        private static partial Regex CreateConnectionStringValidKeyRegex();
//        [GeneratedRegex("^[^\"'=;\\s\\p{Cc}]*$")]
//        private static partial Regex CreateConnectionStringQuoteValueRegex();
//        [GeneratedRegex("^\\{([^\\}\u0000]|\\}\\})*\\}$", RegexOptions.ExplicitCapture)]
//        private static partial Regex CreateConnectionStringQuoteOdbcValueRegex();
//#else
        private static Regex CreateConnectionStringValidKeyRegex() => new Regex("^(?![;\\s])[^\\p{Cc}]+(?<!\\s)$", RegexOptions.Compiled);
        private static Regex CreateConnectionStringQuoteValueRegex() => new Regex("^[^\"'=;\\s\\p{Cc}]*$", RegexOptions.Compiled);
        private static Regex CreateConnectionStringQuoteOdbcValueRegex() => new Regex("^\\{([^\\}\u0000]|\\}\\})*\\}$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
//#endif

        // connection string common keywords
        private static class KEY
        {
            internal const string Integrated_Security = "integrated security";
            internal const string Password = "password";
            internal const string Persist_Security_Info = "persist security info";
            internal const string User_ID = "user id";
        }

        // known connection string common synonyms
        private static class SYNONYM
        {
            internal const string Pwd = "pwd";
            internal const string UID = "uid";
        }

        private readonly string _usersConnectionString;
        private readonly Dictionary<string, string?> _parsetable;
        internal readonly NameValuePair? _keyChain;
        internal readonly bool _hasPasswordKeyword;

        public string UsersConnectionString(bool hidePassword) =>
            UsersConnectionString(hidePassword, false);

        private string UsersConnectionString(bool hidePassword, bool forceHidePassword)
        {
            string connectionString = _usersConnectionString;
            if (_hasPasswordKeyword && (forceHidePassword || (hidePassword && !HasPersistablePassword)))
            {
                ReplacePasswordPwd(out connectionString, false);
            }
            return connectionString ?? string.Empty;
        }

        internal bool HasPersistablePassword => _hasPasswordKeyword ?
            ConvertValueToBoolean(KEY.Persist_Security_Info, false) :
            true; // no password means persistable password so we don't have to munge

        public bool ConvertValueToBoolean(string keyName, bool defaultValue)
        {
            string? value;
            // TODO: Is it possible for _parsetable to contain a null value here? If so there's a bug here, investigate.
            return _parsetable.TryGetValue(keyName, out value) ?
                ConvertValueToBooleanInternal(keyName, value!) :
                defaultValue;
        }

        internal static bool ConvertValueToBooleanInternal(string keyName, string stringValue)
        {
            if (CompareInsensitiveInvariant(stringValue, "true") || CompareInsensitiveInvariant(stringValue, "yes"))
                return true;
            else if (CompareInsensitiveInvariant(stringValue, "false") || CompareInsensitiveInvariant(stringValue, "no"))
                return false;
            else
            {
                string tmp = stringValue.Trim();  // Remove leading & trailing whitespace.
                if (CompareInsensitiveInvariant(tmp, "true") || CompareInsensitiveInvariant(tmp, "yes"))
                    return true;
                else if (CompareInsensitiveInvariant(tmp, "false") || CompareInsensitiveInvariant(tmp, "no"))
                    return false;
                else
                {
                    throw ADP.InvalidConnectionOptionValue(keyName);
                }
            }
        }

        private static bool CompareInsensitiveInvariant(string strvalue, string strconst) =>
            (0 == StringComparer.OrdinalIgnoreCase.Compare(strvalue, strconst));

        [System.Diagnostics.Conditional("DEBUG")]
        static partial void DebugTraceKeyValuePair(string keyname, string? keyvalue, Dictionary<string, string>? synonyms);

        private static string GetKeyName(StringBuilder buffer)
        {
            int count = buffer.Length;
            while ((0 < count) && char.IsWhiteSpace(buffer[count - 1]))
            {
                count--; // trailing whitespace
            }
            return buffer.ToString(0, count).ToLowerInvariant();
        }

        private static string GetKeyValue(StringBuilder buffer, bool trimWhitespace)
        {
            int count = buffer.Length;
            int index = 0;
            if (trimWhitespace)
            {
                while ((index < count) && char.IsWhiteSpace(buffer[index]))
                {
                    index++; // leading whitespace
                }
                while ((0 < count) && char.IsWhiteSpace(buffer[count - 1]))
                {
                    count--; // trailing whitespace
                }
            }
            return buffer.ToString(index, count - index);
        }

        // transition states used for parsing
        private enum ParserState
        {
            NothingYet = 1,   //start point
            Key,
            KeyEqual,
            KeyEnd,
            UnquotedValue,
            DoubleQuoteValue,
            DoubleQuoteValueQuote,
            SingleQuoteValue,
            SingleQuoteValueQuote,
            BraceQuoteValue,
            BraceQuoteValueQuote,
            QuotedValueEnd,
            NullTermination,
        };

        internal static int GetKeyValuePair(string connectionString, int currentPosition, StringBuilder buffer, bool useOdbcRules, out string? keyname, out string? keyvalue)
        {
            int startposition = currentPosition;

            buffer.Length = 0;
            keyname = null;
            keyvalue = null;

            char currentChar = '\0';

            ParserState parserState = ParserState.NothingYet;
            int length = connectionString.Length;
            for (; currentPosition < length; ++currentPosition)
            {
                currentChar = connectionString[currentPosition];

                switch (parserState)
                {
                    case ParserState.NothingYet: // [\\s;]*
                        if ((';' == currentChar) || char.IsWhiteSpace(currentChar))
                        {
                            continue;
                        }
                        if ('\0' == currentChar)
                        { parserState = ParserState.NullTermination; continue; }
                        if (char.IsControl(currentChar))
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        startposition = currentPosition;
                        if ('=' != currentChar)
                        {
                            parserState = ParserState.Key;
                            break;
                        }
                        else
                        {
                            parserState = ParserState.KeyEqual;
                            continue;
                        }

                    case ParserState.Key: // (?<key>([^=\\s\\p{Cc}]|\\s+[^=\\s\\p{Cc}]|\\s+==|==)+)
                        if ('=' == currentChar)
                        { parserState = ParserState.KeyEqual; continue; }
                        if (char.IsWhiteSpace(currentChar))
                        { break; }
                        if (char.IsControl(currentChar))
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        break;

                    case ParserState.KeyEqual: // \\s*=(?!=)\\s*
                        if (!useOdbcRules && '=' == currentChar)
                        { parserState = ParserState.Key; break; }
                        keyname = GetKeyName(buffer);
                        if (string.IsNullOrEmpty(keyname))
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        buffer.Length = 0;
                        parserState = ParserState.KeyEnd;
                        goto case ParserState.KeyEnd;

                    case ParserState.KeyEnd:
                        if (char.IsWhiteSpace(currentChar))
                        { continue; }
                        if (useOdbcRules)
                        {
                            if ('{' == currentChar)
                            { parserState = ParserState.BraceQuoteValue; break; }
                        }
                        else
                        {
                            if ('\'' == currentChar)
                            { parserState = ParserState.SingleQuoteValue; continue; }
                            if ('"' == currentChar)
                            { parserState = ParserState.DoubleQuoteValue; continue; }
                        }
                        if (';' == currentChar)
                        { goto ParserExit; }
                        if ('\0' == currentChar)
                        { goto ParserExit; }
                        if (char.IsControl(currentChar))
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        parserState = ParserState.UnquotedValue;
                        break;

                    case ParserState.UnquotedValue: // "((?![\"'\\s])" + "([^;\\s\\p{Cc}]|\\s+[^;\\s\\p{Cc}])*" + "(?<![\"']))"
                        if (char.IsWhiteSpace(currentChar))
                        { break; }
                        if (char.IsControl(currentChar) || ';' == currentChar)
                        { goto ParserExit; }
                        break;

                    case ParserState.DoubleQuoteValue: // "(\"([^\"\u0000]|\"\")*\")"
                        if ('"' == currentChar)
                        { parserState = ParserState.DoubleQuoteValueQuote; continue; }
                        if ('\0' == currentChar)
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        break;

                    case ParserState.DoubleQuoteValueQuote:
                        if ('"' == currentChar)
                        { parserState = ParserState.DoubleQuoteValue; break; }
                        keyvalue = GetKeyValue(buffer, false);
                        parserState = ParserState.QuotedValueEnd;
                        goto case ParserState.QuotedValueEnd;

                    case ParserState.SingleQuoteValue: // "('([^'\u0000]|'')*')"
                        if ('\'' == currentChar)
                        { parserState = ParserState.SingleQuoteValueQuote; continue; }
                        if ('\0' == currentChar)
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        break;

                    case ParserState.SingleQuoteValueQuote:
                        if ('\'' == currentChar)
                        { parserState = ParserState.SingleQuoteValue; break; }
                        keyvalue = GetKeyValue(buffer, false);
                        parserState = ParserState.QuotedValueEnd;
                        goto case ParserState.QuotedValueEnd;

                    case ParserState.BraceQuoteValue: // "(\\{([^\\}\u0000]|\\}\\})*\\})"
                        if ('}' == currentChar)
                        { parserState = ParserState.BraceQuoteValueQuote; break; }
                        if ('\0' == currentChar)
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        break;

                    case ParserState.BraceQuoteValueQuote:
                        if ('}' == currentChar)
                        { parserState = ParserState.BraceQuoteValue; break; }
                        keyvalue = GetKeyValue(buffer, false);
                        parserState = ParserState.QuotedValueEnd;
                        goto case ParserState.QuotedValueEnd;

                    case ParserState.QuotedValueEnd:
                        if (char.IsWhiteSpace(currentChar))
                        { continue; }
                        if (';' == currentChar)
                        { goto ParserExit; }
                        if ('\0' == currentChar)
                        { parserState = ParserState.NullTermination; continue; }
                        throw ADP.ConnectionStringSyntax(startposition);  // unbalanced single quote

                    case ParserState.NullTermination: // [\\s;\u0000]*
                        if ('\0' == currentChar)
                        { continue; }
                        if (char.IsWhiteSpace(currentChar))
                        { continue; }
                        throw ADP.ConnectionStringSyntax(currentPosition);

                    default:
                        throw ADP.InternalError();
                }
                buffer.Append(currentChar);
            }
        ParserExit:
            switch (parserState)
            {
                case ParserState.Key:
                case ParserState.DoubleQuoteValue:
                case ParserState.SingleQuoteValue:
                case ParserState.BraceQuoteValue:
                    // keyword not found/unbalanced double/single quote
                    throw ADP.ConnectionStringSyntax(startposition);

                case ParserState.KeyEqual:
                    // equal sign at end of line
                    keyname = GetKeyName(buffer);
                    if (string.IsNullOrEmpty(keyname))
                    { throw ADP.ConnectionStringSyntax(startposition); }
                    break;

                case ParserState.UnquotedValue:
                    // unquoted value at end of line
                    keyvalue = GetKeyValue(buffer, true);

                    char tmpChar = keyvalue[keyvalue.Length - 1];
                    if (!useOdbcRules && (('\'' == tmpChar) || ('"' == tmpChar)))
                    {
                        throw ADP.ConnectionStringSyntax(startposition);    // unquoted value must not end in quote, except for odbc
                    }
                    break;

                case ParserState.DoubleQuoteValueQuote:
                case ParserState.SingleQuoteValueQuote:
                case ParserState.BraceQuoteValueQuote:
                case ParserState.QuotedValueEnd:
                    // quoted value at end of line
                    keyvalue = GetKeyValue(buffer, false);
                    break;

                case ParserState.NothingYet:
                case ParserState.KeyEnd:
                case ParserState.NullTermination:
                    // do nothing
                    break;

                default:
                    throw ADP.InternalError();
            }
            if ((';' == currentChar) && (currentPosition < connectionString.Length))
            {
                currentPosition++;
            }
            return currentPosition;
        }

#pragma warning disable CA2249 // Consider using 'string.Contains' instead of 'string.IndexOf'. This file is built into libraries that don't have string.Contains(char).
        private static bool IsValueValidInternal(string? keyvalue)
        {
            if (null != keyvalue)
            {
                return (-1 == keyvalue.IndexOf('\u0000')); // string.Contains(char) is .NetCore2.1+ specific
            }
            return true;
        }

        private static bool IsKeyNameValid([NotNullWhen(true)] string? keyname)
        {
            if (null != keyname)
            {
#if DEBUG
                bool compValue = s_connectionStringValidKeyRegex.IsMatch(keyname);
                Debug.Assert(((0 < keyname.Length) && (';' != keyname[0]) && !char.IsWhiteSpace(keyname[0]) && (-1 == keyname.IndexOf('\u0000'))) == compValue, "IsValueValid mismatch with regex");
#endif
                // string.Contains(char) is .NetCore2.1+ specific
                return ((0 < keyname.Length) && (';' != keyname[0]) && !char.IsWhiteSpace(keyname[0]) && (-1 == keyname.IndexOf('\u0000')));
            }
            return false;
        }
#pragma warning restore CA2249 // Consider using 'string.Contains' instead of 'string.IndexOf'

#if DEBUG
        private static Dictionary<string, string> SplitConnectionString(string connectionString, Dictionary<string, string>? synonyms, bool firstKey)
        {
            var parsetable = new Dictionary<string, string>();
            Regex parser = (firstKey ? s_connectionStringRegexOdbc : s_connectionStringRegex);

            const int KeyIndex = 1, ValueIndex = 2;
            Debug.Assert(KeyIndex == parser.GroupNumberFromName("key"), "wrong key index");
            Debug.Assert(ValueIndex == parser.GroupNumberFromName("value"), "wrong value index");

            if (null != connectionString)
            {
                Match match = parser.Match(connectionString);
                if (!match.Success || (match.Length != connectionString.Length))
                {
                    throw ADP.ConnectionStringSyntax(match.Length);
                }
                int indexValue = 0;
                CaptureCollection keyvalues = match.Groups[ValueIndex].Captures;
                foreach (Capture keypair in match.Groups[KeyIndex].Captures)
                {
                    string keyname = (firstKey ? keypair.Value : keypair.Value.Replace("==", "=")).ToLowerInvariant();
                    string? keyvalue = keyvalues[indexValue++].Value;
                    if (0 < keyvalue.Length)
                    {
                        if (!firstKey)
                        {
                            switch (keyvalue[0])
                            {
                                case '\"':
                                    keyvalue = keyvalue.Substring(1, keyvalue.Length - 2).Replace("\"\"", "\"");
                                    break;
                                case '\'':
                                    keyvalue = keyvalue.Substring(1, keyvalue.Length - 2).Replace("\'\'", "\'");
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        keyvalue = null;
                    }
                    DebugTraceKeyValuePair(keyname, keyvalue, synonyms);
                    string? synonym;
                    string? realkeyname = null != synonyms ?
                        (synonyms.TryGetValue(keyname, out synonym) ? synonym : null) : keyname;

                    if (!IsKeyNameValid(realkeyname))
                    {
                        throw ADP.KeywordNotSupported(keyname);
                    }
                    if (!firstKey || !parsetable.ContainsKey(realkeyname))
                    {
                        parsetable[realkeyname] = keyvalue!; // last key-value pair wins (or first)
                    }
                }
            }
            return parsetable;
        }

        private static void ParseComparison(Dictionary<string, string?> parsetable, string connectionString, Dictionary<string, string>? synonyms, bool firstKey, Exception? e)
        {
            try
            {
                var parsedvalues = SplitConnectionString(connectionString, synonyms, firstKey);
                foreach (var entry in parsedvalues)
                {
                    string keyname = entry.Key;
                    string value1 = entry.Value;
                    string? value2;
                    bool parsetableContainsKey = parsetable.TryGetValue(keyname, out value2);
                    Debug.Assert(parsetableContainsKey, $"{nameof(ParseInternal)} code vs. regex mismatch keyname <{keyname}>");
                    Debug.Assert(value1 == value2, $"{nameof(ParseInternal)} code vs. regex mismatch keyvalue <{value1}> <{value2}>");
                }
            }
            catch (ArgumentException f)
            {
                if (null != e)
                {
                    string msg1 = e.Message;
                    string msg2 = f.Message;

                    const string KeywordNotSupportedMessagePrefix = "Keyword not supported:";
                    const string WrongFormatMessagePrefix = "Format of the initialization string";
                    bool isEquivalent = (msg1 == msg2);
                    if (!isEquivalent)
                    {
                        // We also accept cases were Regex parser (debug only) reports "wrong format" and
                        // retail parsing code reports format exception in different location or "keyword not supported"
                        if (msg2.StartsWith(WrongFormatMessagePrefix, StringComparison.Ordinal))
                        {
                            if (msg1.StartsWith(KeywordNotSupportedMessagePrefix, StringComparison.Ordinal) || msg1.StartsWith(WrongFormatMessagePrefix, StringComparison.Ordinal))
                            {
                                isEquivalent = true;
                            }
                        }
                    }
                    Debug.Assert(isEquivalent, $"ParseInternal code vs regex message mismatch: <{msg1}> <{msg2}>");
                }
                else
                {
                    Debug.Fail($"ParseInternal code vs regex throw mismatch {f.Message}");
                }
                e = null;
            }
            if (null != e)
            {
                Debug.Fail("ParseInternal code threw exception vs regex mismatch");
            }
        }
#endif

        private static NameValuePair? ParseInternal(Dictionary<string, string?> parsetable, string connectionString, bool buildChain, Dictionary<string, string>? synonyms, bool firstKey)
        {
            Debug.Assert(null != connectionString, "null connectionstring");
            StringBuilder buffer = new StringBuilder();
            NameValuePair? localKeychain = null, keychain = null;
#if DEBUG
            try
            {
#endif
                int nextStartPosition = 0;
                int endPosition = connectionString.Length;
                while (nextStartPosition < endPosition)
                {
                    int startPosition = nextStartPosition;

                    string? keyname, keyvalue;
                    nextStartPosition = GetKeyValuePair(connectionString, startPosition, buffer, firstKey, out keyname, out keyvalue);
                    if (string.IsNullOrEmpty(keyname))
                    {
                        break;
                    }
#if DEBUG
                    DebugTraceKeyValuePair(keyname, keyvalue, synonyms);

                    Debug.Assert(IsKeyNameValid(keyname), "ParseFailure, invalid keyname");
                    Debug.Assert(IsValueValidInternal(keyvalue), "parse failure, invalid keyvalue");
#endif
                    string? synonym;
                    string? realkeyname = null != synonyms ?
                        (synonyms.TryGetValue(keyname, out synonym) ? synonym : null) :
                        keyname;

                    if (!IsKeyNameValid(realkeyname))
                    {
                        throw ADP.KeywordNotSupported(keyname);
                    }
                    if (!firstKey || !parsetable.ContainsKey(realkeyname))
                    {
                        parsetable[realkeyname] = keyvalue; // last key-value pair wins (or first)
                    }

                    if (null != localKeychain)
                    {
                        localKeychain = localKeychain.Next = new NameValuePair(realkeyname, keyvalue, nextStartPosition - startPosition);
                    }
                    else if (buildChain)
                    {
                        // first time only - don't contain modified chain from UDL file
                        keychain = localKeychain = new NameValuePair(realkeyname, keyvalue, nextStartPosition - startPosition);
                    }
                }
#if DEBUG
            }
            catch (ArgumentException e)
            {
                ParseComparison(parsetable, connectionString, synonyms, firstKey, e);
                throw;
            }
            ParseComparison(parsetable, connectionString, synonyms, firstKey, null);
#endif
            return keychain;
        }

        internal NameValuePair? ReplacePasswordPwd(out string constr, bool fakePassword)
        {
            bool expanded = false;
            int copyPosition = 0;
            NameValuePair? head = null, tail = null, next = null;
            StringBuilder builder = new StringBuilder(_usersConnectionString.Length);
            for (NameValuePair? current = _keyChain; null != current; current = current.Next)
            {
                if ((KEY.Password != current.Name) && (SYNONYM.Pwd != current.Name))
                {
                    builder.Append(_usersConnectionString, copyPosition, current.Length);
                    if (fakePassword)
                    {
                        next = new NameValuePair(current.Name, current.Value, current.Length);
                    }
                }
                else if (fakePassword)
                {
                    // replace user password/pwd value with *
                    const string equalstar = "=*;";
                    builder.Append(current.Name).Append(equalstar);
                    next = new NameValuePair(current.Name, "*", current.Name.Length + equalstar.Length);
                    expanded = true;
                }
                else
                {
                    // drop the password/pwd completely in returning for user
                    expanded = true;
                }

                if (fakePassword)
                {
                    if (null != tail)
                    {
                        tail = tail.Next = next;
                    }
                    else
                    {
                        tail = head = next;
                    }
                }
                copyPosition += current.Length;
            }
            Debug.Assert(expanded, "password/pwd was not removed");
            constr = builder.ToString();
            return head;
        }
    }

    internal static partial class ADP
    {

        internal static ArgumentNullException ArgumentNull(string parameter)
        {
            ArgumentNullException e = new ArgumentNullException(parameter);
            return e;
        }

        internal static void CheckArgumentNull([NotNull] object? value, string parameterName)
        {
            if (null == value)
            {
                throw ArgumentNull(parameterName);
            }
        }

        internal static Exception InvalidConnectionOptionValue(string key) => new InvalidConnectionOptionValueException(key);

        public static Exception KeywordNotSupported(string keyword) => new InvalidValueException(keyword + "Not supported");

        public static Exception ConnectionStringSyntax(int position) => new ConnectionStringSyntaxException(position);

        public static Exception InternalError() => new ExecutionEngineException();

        public static Exception InvalidKeyname(string name) => new InvalidKeyNameException(name);

        public static Exception InvalidValue(string name) => new InvalidValueException(name);


        internal static void CheckArgumentLength(string keyName, string v)
        {
            throw new NotImplementedException();
        }

    }

    [Serializable]
    public class InvalidValueException : Exception
    {
        public InvalidValueException() { }
        public InvalidValueException(string message) : base(message) { }
        public InvalidValueException(string message, Exception inner) : base(message, inner) { }
        protected InvalidValueException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class InvalidKeyNameException : Exception
    {
        public InvalidKeyNameException() { }
        public InvalidKeyNameException(string message) : base(message) { }
        public InvalidKeyNameException(string message, Exception inner) : base(message, inner) { }
        protected InvalidKeyNameException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class InvalidConnectionOptionValueException : Exception
    {
        public InvalidConnectionOptionValueException() { }
        public InvalidConnectionOptionValueException(string message) : base(message) { }
        public InvalidConnectionOptionValueException(string message, Exception inner) : base(message, inner) { }
        protected InvalidConnectionOptionValueException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ConnectionStringSyntaxException : Exception
    {
        public ConnectionStringSyntaxException() { }
        public ConnectionStringSyntaxException(int position) : base($"syntax error at position {position}") { }
        public ConnectionStringSyntaxException(string message, Exception inner) : base(message, inner) { }
        protected ConnectionStringSyntaxException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

}
