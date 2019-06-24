using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using ResumeParserApp.Model;

namespace ResumeParserApp.Helpers
{
    public class CVReader
    {
        public static string[] GetContentFromPdf(Resume resume, Stream stream)
        {
            StringBuilder stringBuilder = new StringBuilder();

            using (PdfReader pdfReader = new PdfReader(stream))
            {
                for (int i = 1; i <= pdfReader.NumberOfPages; i++)
                {
                    string thePage = PdfTextExtractor.GetTextFromPage(pdfReader, i, new SimpleTextExtractionStrategy());
                    string[] theLines = thePage.Split("\n");

                    foreach (string theLine in theLines)
                    {
                        if (!string.IsNullOrEmpty(theLine) && !string.IsNullOrWhiteSpace(theLine))
                        {
                            stringBuilder.AppendLine(theLine.Trim());
                        }
                    }
                }

                int n = pdfReader.XrefSize; //number of objects in pdf document
                FileStream fs = null;
                PRStream pst;
                PdfImageObject pio;
                PdfObject po;
                String path = @"C:\Users\rka\Desktop\ResumeParserApp\ResumeParserApp\ResumeParserApp";

                try
                {
                    for (int i = 0; i < n; i++)
                    {
                        po = pdfReader.GetPdfObject(i); //get the object at the index i in the objects collection
                        if (po == null || !po.IsStream()) //object not found so continue
                            continue;

                        pst = (PRStream)po; //cast object to stream
                        PdfObject type = pst.Get(PdfName.SUBTYPE); //get the object type
                        //check if the object is the image type object
                        if (type != null && type.ToString().Equals(PdfName.IMAGE.ToString()))
                        {
                            pio = new PdfImageObject(pst); //get the image
                            int imageLength = pio.GetImageAsBytes().Length;
                            if (imageLength != 14593 && imageLength > resume?.PictureData?.Length)  // When convert Doc to Pdf watermark size
                            {
                                fs = new FileStream(path + "image" + i + ".jpg", FileMode.Create);
                                //read bytes of image in to an array
                                resume.PictureData = pio.GetImageAsBytes();
                                //write the bytes array to file
                                fs.Write(resume.PictureData, 0, resume.PictureData.Length);
                                fs.Flush();
                                fs.Close();
                            }      
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return stringBuilder.ToString().Split("\r\n");
        }
    }
}
