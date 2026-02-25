# Uncomment below line to save PDF file in Downloads folder of the project.
### string rootPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
### string filePath = Path.Combine(rootPath, "PDF_" + DateTimeOffset.Now.ToString("HHmmssfff") + ".pdf");
### pdf.SaveAs(filePath);