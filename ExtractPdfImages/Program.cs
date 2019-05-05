using System;
using System.Collections.Generic;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Path = System.IO.Path;

namespace ExtractPdfImages
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            FindImages(args[0], Path.GetDirectoryName(args[0]));
        }

        private static void FindImages(string source, string target)
        {
            using (var pdf = new PdfReader(source))
            {
                for (int pageNumber = 1, imageNumber = 1; pageNumber <= pdf.NumberOfPages; pageNumber++, imageNumber = 1)
                {
                    FindPageImages(pdf.GetPageN(pageNumber), obj =>
                    {
                        if (obj == null)
                            return;

                        var pdfObj = pdf.GetPdfObject(((PRIndirectReference)obj).Number);
                        if (pdfObj == null || !pdfObj.IsStream())
                            return;

                        var stream = (PdfStream)pdfObj;
                        var subtype = stream.Get(PdfName.SUBTYPE);

                        if (subtype == null || !subtype.Equals(PdfName.IMAGE))
                            return;

                        var imageObj = new PdfImageObject((PRStream)stream);

                        using (var image = imageObj.GetDrawingImage())
                        {
                            image.Save(Path.Combine(target, $"Image {pageNumber} - {imageNumber++}.{imageObj.GetFileType()}"));
                        }
                    });
                }
            }
        }

        private static void FindPageImages(PdfDictionary section, Action<PdfObject> callback)
        {
            var resources = (PdfDictionary)PdfReader.GetPdfObject(section.Get(PdfName.RESOURCES));
            var objs = (PdfDictionary)PdfReader.GetPdfObject(resources.Get(PdfName.XOBJECT));
            if (objs == null)
                return;

            foreach (var key in objs.Keys)
            {
                var obj = objs.Get(key);
                if (!obj.IsIndirect())
                    continue;

                var pdfObj = (PdfDictionary)PdfReader.GetPdfObject(obj);
                var type = (PdfName)PdfReader.GetPdfObject(pdfObj.Get(PdfName.SUBTYPE));

                if (PdfName.IMAGE.Equals(type))
                    callback(obj);
                else if (PdfName.FORM.Equals(type) || PdfName.GROUP.Equals(type))
                    FindPageImages(pdfObj, callback);
            }
        }

    }
}
