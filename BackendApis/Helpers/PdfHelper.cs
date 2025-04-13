using iText.Forms;
using iText.Forms.Fields;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

namespace BackendApis.Helpers;

public class PdfHelper
{
    public PdfResponse PreparePDF(Dictionary<string, string> fieldValues, Dictionary<string, string> signatureFields, string filePath)
    {
        PdfResponse response = new PdfResponse();
        try
        {
            if (File.Exists(filePath))
            {
                using (var pdfReader = new PdfReader(filePath))
                using (var memoryStream = new MemoryStream())
                using (var pdfWriter = new PdfWriter(memoryStream))
                {
                    using (var pdfDoc = new PdfDocument(pdfReader, pdfWriter))
                    {
                        var form = PdfAcroForm.GetAcroForm(pdfDoc, true);

                        // Set field values
                        foreach (var fieldValue in fieldValues)
                        {
                            PdfFormField field = form.GetField(fieldValue.Key);
                            if (field != null)
                            {
                                field.SetValue(fieldValue.Value);
                            }
                        }

                        // Add signatures if fields exist
                        foreach (var signatureField in signatureFields)
                        {
                            if (!string.IsNullOrEmpty(signatureField.Key) && !string.IsNullOrEmpty(signatureField.Value))
                            {
                                PdfFormField field = form.GetField(signatureField.Key);
                                if (field != null)
                                {
                                    Rectangle rect = field.GetWidgets()[0].GetRectangle().ToRectangle();
                                    ImageData imageData = ImageDataFactory.Create(Convert.FromBase64String(signatureField.Value));
                                    Image image = new Image(imageData).SetAutoScale(true);
                                    PdfPage page = field.GetWidgets()[0].GetPage();
                                    new Canvas(page, rect).Add(image);
                                    // Optionally, remove the field after adding the image
                                    form.RemoveField(signatureField.Key);
                                }
                            }
                        }

                        // Flatten the form to apply the changes
                        form.FlattenFields();
                    }

                    // Convert the modified PDF to a Base64 string
                    byte[] pdfBytes = memoryStream.ToArray();
                    string base64String = Convert.ToBase64String(pdfBytes);
                    response.IsSuccess = true;
                    response.Message = "success";
                    response.Base64 = base64String;
                }
            }
            else
            {
                response.IsSuccess = false;
                response.Message = "PDF file not found";
            }
        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.Message = ex.Message;
        }
        return response;
    }
}
public class PdfResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public string Base64 { get; set; }
}
