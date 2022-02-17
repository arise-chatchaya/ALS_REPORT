using Access_data.Model;
using Access_data.Utilities;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Access_data.DatasetImg.ImageSet;

namespace Access_data.Service
{
    public class ReportService
    {
        private string DirectoryPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:\\", ""), "Template");
        public string testGetItem()
        {
            return "report is OK";
        }
        public Stream Report01(string PACKAGENO)
        {
            try
            {
                using (masterEntities context = new masterEntities())
                {
                    // Value Process
                    var resultss = context.TB_R_PACKAGE.ToList();
                    var result = context.TB_R_PACKAGE.Where(x => x.Package_No == PACKAGENO).FirstOrDefault();
                    if (result == null)
                    {
                        throw new Exception("CB_Package_No Not Found !");
                    }
                    var AfterJoin = (from trp in context.TB_R_PACKAGE
                                     join trb in context.TB_R_BA on trp.Package_ID equals trb.Package_ID
                                     join tri in context.TB_R_INVOICE on trb.BA_ID equals tri.BA_ID
                                     where trp.Package_No == PACKAGENO
                                     select new
                                     {
                                         trp.Package_No,
                                         trb.BA_No,
                                         tri.Invoice_No,
                                         tri.Invoice_Issue_Date,
                                         trb.BA_ID,
                                         tri.Invoice_Note,
                                         tri.Invoice_Process
                                     }).OrderBy(x => x.Package_No).ThenBy(p => p.BA_No).ToList();
                    //tri.Invoice_Note,
                    //tri.Invoice_Process
                    ReportDocument rpt = new ReportDocument();
                    rpt.Load(Path.Combine(DirectoryPath, "report01.rpt"));
                    // Generate BARCODE
                    BarcodeLib.Barcode b = new BarcodeLib.Barcode();
                    System.Drawing.Image img = b.Encode(BarcodeLib.TYPE.CODE128, AfterJoin.FirstOrDefault().Package_No, Color.Black, Color.White, 375, 120);
                    MemoryStream fs = new MemoryStream();
                    ((Bitmap)img).Save(fs, System.Drawing.Imaging.ImageFormat.Jpeg);
                    byte[] data = fs.ToArray();
                    fs.Dispose();

                    var groupBA = AfterJoin.GroupBy(x => x.BA_No).ToList();
                    List<Report01> resultofGroupCustomModel = new List<Report01>();
                    var BillingAppointment = "Billing Appoint no.";
                    int rowStart = 0;
                    var dt = new Access_data.DatasetImg.ImageSet();
                    dt.DataTable1.Rows.Add(new Object[] { data, data });
                    foreach (var BA_Group in groupBA)
                    {
                        var TEMPCHECKGROUP = string.Empty;
                        foreach (var item in BA_Group)
                        {
                            Report01 itemonKey = new Report01();
                            if (rowStart == 0)
                            {
                                itemonKey.BillingappointmentnoText = BillingAppointment;
                            }
                            else
                            {
                                itemonKey.BillingappointmentnoText = "";
                            }
                            if (string.IsNullOrEmpty(TEMPCHECKGROUP))
                            {
                                itemonKey.BANO = item.BA_No;
                                itemonKey.InvoiceNoText = "Invoice No.";
                            }
                            else
                            {
                                itemonKey.BANO = "";
                                itemonKey.InvoiceNoText = "";
                            }
                            itemonKey.InvoiceNo = item.Invoice_No;
                            itemonKey.InvoiceDate = item.Invoice_Issue_Date != null ? item.Invoice_Issue_Date?.ToString("dd-MMMM-yyyy") : "";
                            itemonKey.InvoiceDateText = "Invoice Date";
                            resultofGroupCustomModel.Add(itemonKey);
                            dt.DataTable2.Rows.Add(new Object[] {
                                itemonKey.BANO,
                                itemonKey.InvoiceNo,
                                itemonKey.InvoiceDate,
                                itemonKey.BillingappointmentnoText,
                                itemonKey.InvoiceDateText,
                                itemonKey.InvoiceNoText ,
                                rowStart
                            });
                            rowStart++;
                        }
                    }
                    
                    result.Invoice_To_Company = result.Invoice_To_Company == null ? "" : result.Invoice_To_Company;
                    result.Invoice_To_Cust_Code = result.Invoice_To_Cust_Code == null ? "" : result.Invoice_To_Cust_Code;
                    result.Invoice_To_Address = result.Invoice_To_Address == null ? "" : result.Invoice_To_Address;
                    result.Invoice_To_Person = result.Invoice_To_Person == null ? "" : result.Invoice_To_Person;
                    result.Package_No = result.Package_No == null ? "" : result.Package_No;
                    var note = AfterJoin.FirstOrDefault().Invoice_Note == null ? "" : AfterJoin.FirstOrDefault().Invoice_Note;
                    var process = AfterJoin.FirstOrDefault().Invoice_Process == null ? "" : AfterJoin.FirstOrDefault().Invoice_Process;
                    rpt.SetDataSource(dt);
                    rpt.SetParameterValue("CompanyName", result.Invoice_To_Company);
                    rpt.SetParameterValue("CustCode", result.Invoice_To_Cust_Code);
                    rpt.SetParameterValue("DeliveryAddress", result.Invoice_To_Address);
                    rpt.SetParameterValue("ReceiverDocument", result.Invoice_To_Person);
                    rpt.SetParameterValue("PackingNo", result.Package_No);
                    rpt.SetParameterValue("InvoiceNote", note); //
                    rpt.SetParameterValue("InvoiceProcess", process);
                    //rpt.SetParameterValue("InvoiceNote", AfterJoin.FirstOrDefault().Invoice_Note); // AfterJoin.FirstOrDefault().Invoice_Note); //groupBA.FirstOrDefault();
                    //rpt.SetParameterValue("InvoiceProcess", AfterJoin.FirstOrDefault().Invoice_Process);  //AfterJoin.FirstOrDefault().Invoice_Process); //groupBA.FirstOrDefault();
                    return rpt.ExportToStream(ExportFormatType.PortableDocFormat);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public Stream Report02(string PACKAGENO)
        {
            try
            {
                using (masterEntities context = new masterEntities())
                {
                    // Report01
                    List<Stream> file = new List<Stream>();
                    var report01 = Report01(PACKAGENO);
                    file.Add(report01);

                    // Report02
                    var result = context.TB_R_PACKAGE.Where(x => x.Package_No == PACKAGENO).FirstOrDefault();
                    if (result == null) throw new Exception("Package_No Not Found !");
                    var BANO_List = context.TB_R_BA.Where(x => x.Package_ID == result.Package_ID).ToList();
                    if (BANO_List.Count == 0) throw new Exception("BANO Not Found !");
                    foreach (var BANO in BANO_List)
                    {
                        var InvoiceList = context.TB_R_INVOICE.Where(x => x.BA_ID == BANO.BA_ID).ToList();
                        var HeaderText1 = BANO;
                        var HeaderText2 = InvoiceList.FirstOrDefault();
                        if (InvoiceList.Count > 0)
                        {
                            // Generate BARCODE
                            BarcodeLib.Barcode b1 = new BarcodeLib.Barcode();
                            System.Drawing.Image img1 = b1.Encode(BarcodeLib.TYPE.CODE128, PACKAGENO, Color.Black, Color.White, 375, 120);
                            MemoryStream fs1 = new MemoryStream();
                            ((Bitmap)img1).Save(fs1, System.Drawing.Imaging.ImageFormat.Jpeg);
                            byte[] Barcode01 = fs1.ToArray();
                            fs1.Dispose();

                            BarcodeLib.Barcode b2 = new BarcodeLib.Barcode();
                            System.Drawing.Image img2 = b2.Encode(BarcodeLib.TYPE.CODE128, BANO.BA_No, Color.Black, Color.White, 375, 120);
                            MemoryStream fs2 = new MemoryStream();
                            ((Bitmap)img2).Save(fs2, System.Drawing.Imaging.ImageFormat.Jpeg);
                            byte[] Barcode02 = fs2.ToArray();
                            fs2.Dispose();

                            ReportDocument rpt = new ReportDocument();
                            rpt.Load(Path.Combine(DirectoryPath, "report02.rpt"));
                            var dt = new Access_data.DatasetImg.ImageSet();
                            dt.DataTable1.Rows.Add(new Object[] { Barcode01, Barcode02 });

                            var index = 1;
                            foreach (var inviceItem in InvoiceList)
                            {
                                dt.DataTable3.Rows.Add(new Object[] {
                            index,
                            inviceItem.Invoice_Issue_Date != null ? inviceItem.Invoice_Issue_Date?.ToString("dd-MM-yyyy") : "",
                            inviceItem.Invoice_No,
                            inviceItem.Total_Invoice_Amount_Inc_Vat ?? 0
                            });
                                index++;
                            }
                            rpt.SetDataSource(dt);
                            rpt.SetParameterValue("QueteNo", HeaderText1.Quote_No == null ? "" : HeaderText1.Quote_No);
                            rpt.SetParameterValue("CreditTerm", HeaderText2.CreditTerm == null ? "" : HeaderText2.CreditTerm);
                            rpt.SetParameterValue("ReportToName", HeaderText1.Report_To_Comany == null ? "" : HeaderText1.Report_To_Comany);
                            rpt.SetParameterValue("ReportToAddress", HeaderText1.Reports_To_Address == null ? "" : HeaderText1.Reports_To_Address);
                            rpt.SetParameterValue("CompanyName", HeaderText1.Invoice_To_Comany == null ? "" : HeaderText1.Invoice_To_Comany);
                            rpt.SetParameterValue("CustCode", HeaderText1.Invoice_Cust_Code == null ? "" : HeaderText1.Invoice_Cust_Code);
                            rpt.SetParameterValue("DeliverToName", HeaderText1.Invoice_To_Person == null ? "" : HeaderText1.Invoice_To_Person);
                            rpt.SetParameterValue("DeliverToAddress", HeaderText1.Invoice_To_Address == null ? "" : HeaderText1.Invoice_To_Address);
                            rpt.SetParameterValue("DeliverToTelphone", HeaderText1.Invoice_To_Tel == null ? "" : HeaderText1.Invoice_To_Tel);
                            rpt.SetParameterValue("PackingNo", PACKAGENO == null ? "" : PACKAGENO);
                            rpt.SetParameterValue("BillingAppointmentNo", HeaderText1.BA_No == null ? "" : HeaderText1.BA_No);
                            rpt.SetParameterValue("IssueDate", HeaderText2.Invoice_Issue_Date?.ToString("MMMM dd, yyyy / HH:mm"));
                            var PDFStream = rpt.ExportToStream(ExportFormatType.PortableDocFormat);
                            file.Add(PDFStream);
                        }
                    }
                    var resultPDF = MergeReports(file);
                    return new MemoryStream(resultPDF);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private byte[] MergeReports(List<Stream> files)
        {
            try
            {
                //MemoryStream finalStream = new MemoryStream();
                //PdfCopyFields copy = new PdfCopyFields(finalStream);
                //var ms1 = files[0];
                //ms1.Position = 0;
                //copy.AddDocument(new PdfReader(ms1));
                //ms1.Dispose();
                //var ms2 = files[1];
                //ms2.Position = 0;
                //copy.AddDocument(new PdfReader(ms2));
                //ms1.Dispose();
                //return finalStream;
                MemoryStream ms = new MemoryStream();
                using (Document doc = new Document())
                {
                    PdfCopy pdf = new PdfCopy(doc, ms);
                    pdf.CloseStream = false;
                    doc.Open();

                    PdfReader reader = null;
                    PdfImportedPage page = null;

                    for (int i = 0; i < files.Count(); i++)
                    {
                        files[i].Position = 0;
                        reader = new PdfReader(files[i]);

                        for (int p = 1; p <= reader.NumberOfPages; p++)
                        {
                            page = pdf.GetImportedPage(reader, p);
                            pdf.AddPage(page);
                            pdf.FreeReader(reader);
                        }
                        reader.Close();
                    }
                    //files[1].Position = 0;
                    //reader = new PdfReader(files[1]);
                    //page = pdf.GetImportedPage(reader, 2);
                    //pdf.AddPage(page);
                    //pdf.FreeReader(reader);
                    //reader.Close();
                    //doc.Close();
                }
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


    }
}
