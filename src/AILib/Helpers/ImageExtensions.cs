namespace AILib.Helpers
{

    public static class ImageExtensions
    {

        public static (ReadOnlyMemory<byte> imageData, string mediaType) LoadImageAndDetectMediaType(string imagePath)
        {

            if (!File.Exists(imagePath))
                throw new FileNotFoundException($"Fichier introuvable : {imagePath}");

            byte[] bytes = File.ReadAllBytes(imagePath);
            var ext = Path.GetExtension(imagePath);
            return (bytes.AsMemory(), ext.ResolveMediaTypeFromExtension());

        }

        public static string ResolveMediaTypeFromExtension(this string ext)
        {
            return ext.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => throw new NotSupportedException($"Extension non supportée : {ext}")
            };
        }

        public static string ResolveExtensionFromMediaType(this string mediaType)
        {
            return mediaType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/bmp" => ".bmp",
                "image/webp" => ".webp",
                _ => throw new NotSupportedException($"Media type non supporté : {mediaType}")
            };
        }

    }


}