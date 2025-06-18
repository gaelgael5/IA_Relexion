namespace AILib.Helpers
{
    public static class IOExtension
    {


        public static IOType EvaluateDocument(this string path)
        {

            if (string.IsNullOrEmpty(path))
                return IOType.None;

            try
            {

                if (File.Exists(path))
                    return IOType.File;

                else if (Directory.Exists(path))
                    return IOType.Folder;

                if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
                    return IOType.Folder;
            
                if (!string.IsNullOrEmpty(Path.GetExtension(path)))
                    return IOType.File;

            }

            catch (Exception) { }

            return IOType.None;

        }


    }


    public enum IOType
    {
        None,
        File,
        Folder,
    }



}