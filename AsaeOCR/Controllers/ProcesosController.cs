using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tesseract;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Text;
using System.Drawing;
using BitMiracle.Docotic.Pdf;

namespace AsaeOCR.Controllers
{
    public class ProcesosController : Controller
    {
        public ActionResult Proceso01()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Proceso01(int modo, int resolucion)
        {
            var rutaArchivo = System.IO.Path.Combine(Server.MapPath("~/PDFs/"), "22_AAV339_002_202112.pdf");
            //Proceso automatizado
            string textoReconocido = "";
            var documentoTexto = new StringBuilder();
            using (var pdf = new BitMiracle.Docotic.Pdf.PdfDocument(rutaArchivo))
            {
                var ruta = @"C:\Users\jlvru\source\repos\AsaeOCR\AsaeOCR\Tesseract\"; //ruta del archivo eng.traineddata
                var mode = (EngineMode)modo;
                using (var motorTesser = new TesseractEngine(ruta, "eng", mode))  //Valores: Default, LstmOnly, TesseractAndLstm, TesseractOnly
                {
                    for (int i = 0; i < pdf.PageCount; ++i)
                    {
                        if (documentoTexto.Length > 0)
                            documentoTexto.Append("\r\n\r\n");

                        BitMiracle.Docotic.Pdf.PdfPage pagina = pdf.Pages[i];
                        string textoBuscable = pagina.GetText();

                        // Simplemente se checa si la página contiene texto buscable.
                        // No se necesita ejecutar OCR en este caso.
                        if (!string.IsNullOrEmpty(textoBuscable.Trim()))
                        {
                            documentoTexto.Append(textoBuscable);
                            continue;
                        }

                        // Esta página no es buscable.
                        // Guarda la página como una imagen con la resolución indicada
                        PdfDrawOptions opciones = PdfDrawOptions.Create();
                        opciones.BackgroundColor = new PdfRgbColor(255, 255, 255);

                        opciones.HorizontalResolution = resolucion;
                        opciones.VerticalResolution = resolucion;
                        opciones.Compression = ImageCompressionOptions.CreateGrayscaleTiff();

                        string paginaImagen = System.IO.Path.Combine(Server.MapPath("~/PDFs/"), $"page_{i}.tif");
                        pagina.Save(paginaImagen, opciones);

                        // Ejecuta OCR
                        using (Pix img = Pix.LoadFromFile(paginaImagen))
                        {
                            using (Page paginaReconocida = motorTesser.Process(img))
                            {
                                //Console.WriteLine($"Mean confidence for page #{i}: {recognizedPage.GetMeanConfidence()}");

                                textoReconocido = paginaReconocida.GetText();
                                documentoTexto.Append(textoReconocido);
                            }
                        }

                        //File.Delete(pageImage);
                    }
                }
            }

            //Guarda archivo de texto obtenido
            using (var writer = new StreamWriter(rutaArchivo))
                writer.Write(documentoTexto.ToString());

            //Cuenta las líneas obtenidas del archivo
            int contador = 0;
            string linea;
            StreamReader file = new StreamReader(rutaArchivo);
            while ((linea = file.ReadLine()) != null)
            {
                contador++;
            }

            file.Close();

            //Obtiene una línea específica del archivo
            int nolinea = 12;
            string lineasolicitada = "";
            using (StreamReader inputFile = new StreamReader(rutaArchivo))
            {
                //Saltar todas las líneas en las que no se esté interesado
                for (int i = 1; i < nolinea; i++)
                {
                    inputFile.ReadLine();
                }

                //Línea específica que se quiere leer
                lineasolicitada = inputFile.ReadLine();
            }

            //Salida al cliente
            ViewBag.Mensaje = documentoTexto.ToString() +
            " <br />Hay " + contador + " líneas en el archivo, de la línea " + nolinea + " se obtuvo: " + lineasolicitada +
            " <br />(" + lineasolicitada.Substring(0, 20) + ")";

            return View();
        }





    }
}
