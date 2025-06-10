using System.Globalization;
using System.Text;
using UglyToad.PdfPig;

namespace AILib.Helpers
{

    public static class OpenPdfExtensions
    {

        public static string ExtractPdfAsMarkdown(string filePath)
        {

            var sb = new StringBuilder();

            using (var document = PdfDocument.Open(filePath))
            {
                var info = document.Information;

                // Métadonnées
                sb.AppendLine("# Informations sur le document");
                sb.AppendLine($"- **Titre** : {info.Title ?? "Inconnu"}");
                sb.AppendLine($"- **Auteur** : {info.Author ?? "Inconnu"}");
                sb.AppendLine($"- **Sujet** : {info.Subject ?? "Inconnu"}");
                sb.AppendLine($"- **Mots-clés** : {info.Keywords ?? "Inconnu"}");
                sb.AppendLine($"- **Créé le** : {info.CreationDate?.ToString(CultureInfo.InvariantCulture) ?? "Inconnu"}");
                sb.AppendLine($"- **Modifié le** : {info.ModifiedDate?.ToString(CultureInfo.InvariantCulture) ?? "Inconnu"}");
                sb.AppendLine();

                // Pages
                foreach (var page in document.GetPages())
                {
                    sb.AppendLine($"## 📄 Page {page.Number}");
                    var lines = page.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                        {
                            sb.AppendLine(trimmed);
                        }
                    }

                    sb.AppendLine(); // Saut de page
                }
            }

            return sb.ToString();

        }

    }


}