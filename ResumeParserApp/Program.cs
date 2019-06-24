using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Org.BouncyCastle.Asn1.Ocsp;
using ResumeParserApp.Helpers;
using ResumeParserApp.Model;
using ResumeParserApp.Process;
using Path = System.IO.Path;

namespace ResumeParserApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            //string path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "CV-2.pdf");
            //string[] lines = File.ReadAllLines(path);

            ResumeProcessor resumeProcessor = new ResumeProcessor();

            Resume resume = new Resume();

            FileStream to_strem = new FileStream(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "sample_resume.doc"), FileMode.Open);
            //byte[] bytes = System.IO.File.ReadAllBytes(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "CV-4.pdf"));
            //string[] lines = CVReader.GetContentFromPdf(resume, new MemoryStream(bytes));
            //resume = resumeProcessor.Parse(lines);

            var c = CVReader.GetContentFromPdf(resume, Helpers.Convert.ToPdfStream(to_strem));

            var index = Array.FindIndex(c, x => x.Contains("Pty Ltd", StringComparison.InvariantCultureIgnoreCase));
            List<string> list = c.ToList();
            list.RemoveRange(0, index + 1);
            c = list.ToArray();

            //var q = CVReader.GetContentFromPdf(resume, new MemoryStream(bytes));
            resume = resumeProcessor.Parse(c);
            Console.ReadKey();
        }
    }
}
