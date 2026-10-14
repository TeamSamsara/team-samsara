// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact.Tests/ContactAttachmentCheckerTests.cs
// Version : 1.0.0
// Latest commit: feat/contact-module
// Author : Gerrah
// Purpose : Proves the attachment checker enforces the count, size and type limits by extension and file signature, and cleans file names.

using System.Text;
using Microsoft.Extensions.Options;
using Shouldly;
using TeamSamsara.Modules.Contact.Handlers;
using TeamSamsara.Modules.Contact.Models;

namespace TeamSamsara.Modules.Contact.Tests;

public class ContactAttachmentCheckerTests
{
    #region Fields

    private const int MaxFiles = 3;
    private const int MaxFileBytes = 100;
    private const int MaxTotalBytes = 150;
    private const int DefaultLength = 20;
    private const int HalfTotalPlusOne = 80;
    private const int LongNameLength = 150;
    private const int WebpTagOffset = 8;

    private static readonly byte[] _pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] _jpegSignature = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] _pdfSignature = Encoding.ASCII.GetBytes("%PDF-");
    private static readonly byte[] _zipSignature = { 0x50, 0x4B, 0x03, 0x04 };

    private readonly ContactAttachmentChecker _checker;

    #endregion

    #region Constructors

    public ContactAttachmentCheckerTests()
    {
        var settings = new ContactSettings { MaxFiles = MaxFiles, MaxFileBytes = MaxFileBytes, MaxTotalBytes = MaxTotalBytes };

        _checker = new ContactAttachmentChecker(Options.Create(settings));
    }

    #endregion

    #region Public Methods

    [Theory]
    [InlineData("photo.png")]
    [InlineData("photo.jpg")]
    [InlineData("photo.jpeg")]
    [InlineData("photo.webp")]
    [InlineData("deck.pdf")]
    [InlineData("letter.docx")]
    [InlineData("slides.pptx")]
    [InlineData("budget.xlsx")]
    [InlineData("PHOTO.PNG")]
    [InlineData("Report.PDF")]
    public void AnAllowedFile_Passes(string fileName)
    {
        var status = _checker.Check([new ContactFile(fileName, ContentFor(fileName))], out var sanitized);

        status.ShouldBeNull();
        sanitized.Count.ShouldBe(1);
    }

    [Fact]
    public void NoFiles_Passes()
    {
        _checker.Check([], out var sanitized).ShouldBeNull();

        sanitized.ShouldBeEmpty();
    }

    [Fact]
    public void TheContentIsKeptUnchanged()
    {
        var content = ContentFor("photo.png");

        _checker.Check([new ContactFile("photo.png", content)], out var sanitized);

        sanitized[0].Content.ShouldBe(content);
    }

    [Fact]
    public void AFileWhoseContentDoesNotMatchItsExtension_IsUnsupported()
    {
        var pdfNamedAsPng = new ContactFile("photo.png", ContentFor("deck.pdf"));

        _checker.Check([pdfNamedAsPng], out _).ShouldBe(ContactStatus.UnsupportedFileType);
    }

    [Theory]
    [InlineData("macro.docm")]
    [InlineData("macro.xlsm")]
    [InlineData("archive.zip")]
    [InlineData("program.exe")]
    [InlineData("noextension")]
    [InlineData("notes.txt")]
    public void ADisallowedExtension_IsUnsupported(string fileName)
    {
        var file = new ContactFile(fileName, ContentFor("letter.docx"));

        _checker.Check([file], out _).ShouldBe(ContactStatus.UnsupportedFileType);
    }

    [Fact]
    public void AnEmptyFile_IsUnsupported()
    {
        _checker.Check([new ContactFile("photo.png", [])], out _).ShouldBe(ContactStatus.UnsupportedFileType);
    }

    [Fact]
    public void AWebpFileWithoutTheWebpTag_IsUnsupported()
    {
        var content = ContentFor("photo.webp");
        Encoding.ASCII.GetBytes("WAVE").CopyTo(content, WebpTagOffset);

        _checker.Check([new ContactFile("photo.webp", content)], out _).ShouldBe(ContactStatus.UnsupportedFileType);
    }

    [Fact]
    public void AWebpFileShorterThanItsHeader_IsUnsupported()
    {
        var content = Encoding.ASCII.GetBytes("RIFF");

        _checker.Check([new ContactFile("photo.webp", content)], out _).ShouldBe(ContactStatus.UnsupportedFileType);
    }

    [Fact]
    public void MoreFilesThanAllowed_AreRejected()
    {
        var files = Enumerable.Range(0, MaxFiles + 1)
            .Select(index => new ContactFile($"photo{index}.png", ContentFor("photo.png")))
            .ToArray();

        _checker.Check(files, out _).ShouldBe(ContactStatus.TooManyFiles);
    }

    [Fact]
    public void AFileAtTheSizeLimit_Passes()
    {
        var file = new ContactFile("photo.png", ContentFor("photo.png", MaxFileBytes));

        _checker.Check([file], out _).ShouldBeNull();
    }

    [Fact]
    public void AFileOverTheSizeLimit_IsTooLarge()
    {
        var file = new ContactFile("photo.png", ContentFor("photo.png", MaxFileBytes + 1));

        _checker.Check([file], out _).ShouldBe(ContactStatus.FileTooLarge);
    }

    [Fact]
    public void FilesTogetherOverTheTotalLimit_AreTooLarge()
    {
        var files = new[]
        {
            new ContactFile("first.png", ContentFor("first.png", HalfTotalPlusOne)),
            new ContactFile("second.png", ContentFor("second.png", HalfTotalPlusOne))
        };

        _checker.Check(files, out _).ShouldBe(ContactStatus.FileTooLarge);
    }

    [Theory]
    [InlineData("../../etc/passwd.png", "passwd.png")]
    [InlineData("C:\\fakepath\\photo.png", "photo.png")]
    [InlineData("bad<name>.png", "bad_name_.png")]
    [InlineData("pho\u0001to.png", "photo.png")]
    [InlineData("  spaced.png  ", "spaced.png")]
    public void FileNames_AreCleaned(string rawName, string expected)
    {
        _checker.Check([new ContactFile(rawName, ContentFor("photo.png"))], out var sanitized).ShouldBeNull();

        sanitized[0].FileName.ShouldBe(expected);
    }

    [Fact]
    public void ALongFileName_IsShortenedButKeepsItsExtension()
    {
        var rawName = new string('a', LongNameLength) + ".png";

        _checker.Check([new ContactFile(rawName, ContentFor("photo.png"))], out var sanitized).ShouldBeNull();

        sanitized[0].FileName.Length.ShouldBe(ContactConstants.MaxFileNameLength);
        sanitized[0].FileName.ShouldEndWith(".png");
    }

    [Fact]
    public void ABlankFileName_IsUnsupported()
    {
        _checker.Check([new ContactFile("   ", ContentFor("photo.png"))], out _).ShouldBe(ContactStatus.UnsupportedFileType);
    }

    #endregion

    #region Private Methods

    // Bytes that start like a real file of the kind the name's extension claims, padded to the length
    private static byte[] ContentFor(string fileName, int length = DefaultLength)
    {
        var content = new byte[length];

        switch (Path.GetExtension(fileName).ToLowerInvariant())
        {
            case ".png":
                _pngSignature.CopyTo(content, 0);
                break;
            case ".jpg" or ".jpeg":
                _jpegSignature.CopyTo(content, 0);
                break;
            case ".webp":
                Encoding.ASCII.GetBytes("RIFF").CopyTo(content, 0);
                Encoding.ASCII.GetBytes("WEBP").CopyTo(content, WebpTagOffset);
                break;
            case ".pdf":
                _pdfSignature.CopyTo(content, 0);
                break;
            default:
                _zipSignature.CopyTo(content, 0);
                break;
        }

        return content;
    }

    #endregion
}
