using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Xml.Linq;

namespace AILib.Helpers
{


    public static class OpenXmlExtensions
    {

        public static List<string> ExtractTextFromSlides(string filePath)
        {

            var slidesText = new List<string>();

            using (PresentationDocument presentationDocument = PresentationDocument.Open(filePath, false))
            {
                var presentationPart = presentationDocument.PresentationPart;
                if (presentationPart?.Presentation == null) return slidesText;

                var slideParts = presentationPart.SlideParts.ToList();
                foreach (var slidePart in slideParts)
                {
                    var slideElement = slidePart.Slide;
                    if (slideElement == null)
                    {
                        slidesText.Add("[Slide vide]");
                        continue;
                    }

                    // Convertit l'objet OpenXmlElement (slide) en XElement (XML lisible)
                    var xml = XElement.Parse(slideElement.OuterXml);
                    slidesText.Add(xml.ToString());
                }
            }

            return slidesText;

        }

        public static List<string> ExtractTextFromWordDocument(string filePath)
        {
            var xmlParagraphs = new List<string>();

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                if (body == null)
                {
                    xmlParagraphs.Add("[Document vide]");
                    return xmlParagraphs;
                }

                // Parcourt chaque paragraphe du document Word
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    try
                    {
                        var xml = XElement.Parse(paragraph.OuterXml);
                        xmlParagraphs.Add(xml.ToString());
                    }
                    catch (Exception ex)
                    {
                        xmlParagraphs.Add($"[Erreur lors de l'analyse XML du paragraphe] {ex.Message}");
                    }
                }
            }

            return xmlParagraphs;
        }

    }


}