// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/Handlers/ContactAttachmentChecker.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Enforces the attachment count, size and type limits and cleans the file names of a contact submission.

using Microsoft.Extensions.Options;
using TeamSamsara.Modules.Contact.Models;

namespace TeamSamsara.Modules.Contact.Handlers;

public class ContactAttachmentChecker
{
    #region Fields

    private const string FallbackFileName = "attachment";
    private const string ForbiddenNameCharacters = "<>:\"|?*\\/";
    private const char ReplacementCharacter = '_';
    private const int WebpTagOffset = 8;
    private const int WebpMinimumLength = 12;

    private const string PngExtension = ".png";
    private const string JpegExtension = ".jpeg";
    private const string JpgExtension = ".jpg";
    private const string WebpExtension = ".webp";
    private const string PdfExtension = ".pdf";
    private const string DocxExtension = ".docx";
    private const string PptxExtension = ".pptx";
    private const string XlsxExtension = ".xlsx";

    private static readonly byte[] _pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] _jpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] _riffSignature = "RIFF"u8.ToArray();
    private static readonly byte[] _webpTag = "WEBP"u8.ToArray();
    private static readonly byte[] _pdfSignature = "%PDF-"u8.ToArray();
    private static readonly byte[] _zipSignature = [0x50, 0x4B, 0x03, 0x04];

    private readonly ContactSettings _settings;

    #endregion

    #region Constructors

    public ContactAttachmentChecker(IOptions<ContactSettings> settings)
    {
        _settings = settings.Value;
    }

    #endregion

    #region Public Methods

    // Returns null when every file is acceptable, otherwise the status of the first problem; cleaned files come back in sanitized
    public ContactStatus? Check(IReadOnlyList<ContactFile> files, out IReadOnlyList<ContactFile> sanitized)
    {
        sanitized = Array.Empty<ContactFile>();

        if (files.Count > _settings.MaxFiles)
        {
            return ContactStatus.TooManyFiles;
        }

        long totalBytes = 0;
        var cleaned = new List<ContactFile>(files.Count);

        foreach (var file in files)
        {
            if (file.Content.Length > _settings.MaxFileBytes)
            {
                return ContactStatus.FileTooLarge;
            }

            totalBytes += file.Content.Length;
            if (totalBytes > _settings.MaxTotalBytes)
            {
                return ContactStatus.FileTooLarge;
            }

            var fileName = SanitizeFileName(file.FileName);
            if (!HasExpectedSignature(Path.GetExtension(fileName).ToLowerInvariant(), file.Content))
            {
                return ContactStatus.UnsupportedFileType;
            }

            cleaned.Add(new ContactFile(fileName, file.Content));
        }

        sanitized = cleaned;
        return null;
    }

    #endregion

    #region Private Methods

    // Strips any path, control characters and forbidden characters, then caps the length while keeping the extension
    private static string SanitizeFileName(string rawName)
    {
        var name = rawName.Replace('\\', '/');
        name = Path.GetFileName(name);

        var characters = name.Where(character => !char.IsControl(character))
            .Select(character => ForbiddenNameCharacters.Contains(character) ? ReplacementCharacter : character)
            .ToArray();
        name = new string(characters).Trim();

        if (name.Length == 0)
        {
            return FallbackFileName;
        }

        if (name.Length > ContactConstants.MaxFileNameLength)
        {
            var extension = Path.GetExtension(name);
            var stemLength = Math.Max(0, ContactConstants.MaxFileNameLength - extension.Length);
            name = (name[..stemLength] + extension)[..Math.Min(ContactConstants.MaxFileNameLength, stemLength + extension.Length)];
        }

        return name;
    }

    // True when the extension is allowed and the file starts with the bytes that kind of file must start with
    private static bool HasExpectedSignature(string extension, byte[] content)
    {
        var bytes = content.AsSpan();

        return extension switch
        {
            PngExtension => bytes.StartsWith(_pngSignature),
            JpgExtension or JpegExtension => bytes.StartsWith(_jpegSignature),
            WebpExtension => IsWebp(bytes),
            PdfExtension => bytes.StartsWith(_pdfSignature),
            DocxExtension or PptxExtension or XlsxExtension => bytes.StartsWith(_zipSignature),
            _ => false
        };
    }

    // True when the bytes open with a RIFF header whose format tag is WEBP
    private static bool IsWebp(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= WebpMinimumLength
            && bytes.StartsWith(_riffSignature)
            && bytes.Slice(WebpTagOffset, _webpTag.Length).SequenceEqual(_webpTag);
    }

    #endregion
}
