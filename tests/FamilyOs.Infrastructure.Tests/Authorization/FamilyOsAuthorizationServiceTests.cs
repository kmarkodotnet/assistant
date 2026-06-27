using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Authorization;
using FluentAssertions;
using NSubstitute;

namespace FamilyOs.Infrastructure.Tests.Authorization;

public sealed class FamilyOsAuthorizationServiceTests
{
    private static Document CreateDocument(
        Guid createdByUserAccountId,
        Guid? relatedFamilyMemberId = null,
        bool isPrivate = false)
    {
        return Document.Create(
            title: "Test",
            originalFileName: "test.pdf",
            mimeType: "application/pdf",
            sizeBytes: 100,
            storagePath: "2026/01/test.pdf",
            sha256: new string('a', 64),
            sourceType: SourceType.Upload,
            origin: Origin.Manual,
            createdByUserAccountId: createdByUserAccountId,
            relatedFamilyMemberId: relatedFamilyMemberId,
            isPrivate: isPrivate);
    }

    private static MedicalRecord CreateMedicalRecord(Guid familyMemberId)
    {
        return MedicalRecord.Create(
            documentId: Guid.NewGuid(),
            familyMemberId: familyMemberId,
            recordType: MedicalRecordType.Prescription,
            recordDate: DateOnly.FromDateTime(DateTime.UtcNow),
            title: "Test Record");
    }

    [Fact]
    public void CanReadDocument_Admin_ReturnsTrue()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.Role.Returns(nameof(UserRole.Admin));
        var svc = new FamilyOsAuthorizationService(currentUser);
        var doc = CreateDocument(Guid.NewGuid(), isPrivate: true);

        // Act & Assert
        svc.CanReadDocument(doc).Should().BeTrue();
    }

    [Fact]
    public void CanReadDocument_AdultOwnDocument_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.Role.Returns(nameof(UserRole.Adult));
        currentUser.UserAccountId.Returns(userId);
        var svc = new FamilyOsAuthorizationService(currentUser);
        var doc = CreateDocument(userId, isPrivate: true);

        // Act & Assert
        svc.CanReadDocument(doc).Should().BeTrue();
    }

    [Fact]
    public void CanReadDocument_AdultOtherPublicDocument_ReturnsTrue()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.Role.Returns(nameof(UserRole.Adult));
        currentUser.UserAccountId.Returns(Guid.NewGuid());
        var svc = new FamilyOsAuthorizationService(currentUser);
        var doc = CreateDocument(Guid.NewGuid(), isPrivate: false);

        // Act & Assert
        svc.CanReadDocument(doc).Should().BeTrue();
    }

    [Fact]
    public void CanReadDocument_AdultOtherPrivateDocument_ReturnsFalse()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.Role.Returns(nameof(UserRole.Adult));
        currentUser.UserAccountId.Returns(Guid.NewGuid());
        var svc = new FamilyOsAuthorizationService(currentUser);
        var doc = CreateDocument(Guid.NewGuid(), isPrivate: true);

        // Act & Assert
        svc.CanReadDocument(doc).Should().BeFalse();
    }

    [Fact]
    public void CanReadDocument_ChildRelatedPublicDocument_ReturnsTrue()
    {
        // Arrange
        var familyMemberId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.Role.Returns(nameof(UserRole.Child));
        currentUser.FamilyMemberId.Returns(familyMemberId);
        var svc = new FamilyOsAuthorizationService(currentUser);
        var doc = CreateDocument(Guid.NewGuid(), relatedFamilyMemberId: familyMemberId, isPrivate: false);

        // Act & Assert
        svc.CanReadDocument(doc).Should().BeTrue();
    }

    [Fact]
    public void CanReadDocument_ChildRelatedPrivateDocument_ReturnsFalse()
    {
        // Arrange
        var familyMemberId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.Role.Returns(nameof(UserRole.Child));
        currentUser.FamilyMemberId.Returns(familyMemberId);
        var svc = new FamilyOsAuthorizationService(currentUser);
        var doc = CreateDocument(Guid.NewGuid(), relatedFamilyMemberId: familyMemberId, isPrivate: true);

        // Act & Assert
        svc.CanReadDocument(doc).Should().BeFalse();
    }

    [Fact]
    public void CanReadDocument_ChildUnrelatedDocument_ReturnsFalse()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.Role.Returns(nameof(UserRole.Child));
        currentUser.FamilyMemberId.Returns(Guid.NewGuid());
        var svc = new FamilyOsAuthorizationService(currentUser);
        var doc = CreateDocument(Guid.NewGuid(), relatedFamilyMemberId: Guid.NewGuid(), isPrivate: false);

        // Act & Assert
        svc.CanReadDocument(doc).Should().BeFalse();
    }

    [Fact]
    public void CanWriteDocument_AdultOwnDocument_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.Role.Returns(nameof(UserRole.Adult));
        currentUser.UserAccountId.Returns(userId);
        var svc = new FamilyOsAuthorizationService(currentUser);
        var doc = CreateDocument(userId);

        // Act & Assert
        svc.CanWriteDocument(doc).Should().BeTrue();
    }

    [Fact]
    public void CanWriteDocument_AdultOtherDocument_ReturnsFalse()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.Role.Returns(nameof(UserRole.Adult));
        currentUser.UserAccountId.Returns(Guid.NewGuid());
        var svc = new FamilyOsAuthorizationService(currentUser);
        var doc = CreateDocument(Guid.NewGuid());

        // Act & Assert
        svc.CanWriteDocument(doc).Should().BeFalse();
    }

    [Fact]
    public void CanWriteDocument_Child_ReturnsFalse()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.Role.Returns(nameof(UserRole.Child));
        var svc = new FamilyOsAuthorizationService(currentUser);
        var doc = CreateDocument(Guid.NewGuid());

        // Act & Assert
        svc.CanWriteDocument(doc).Should().BeFalse();
    }

    [Fact]
    public void CanReadMedicalRecord_AdminUser_ReturnsTrue()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.Role.Returns(nameof(UserRole.Admin));
        var svc = new FamilyOsAuthorizationService(currentUser);
        var record = CreateMedicalRecord(Guid.NewGuid());

        // Act & Assert
        svc.CanReadMedicalRecord(record).Should().BeTrue();
    }

    [Fact]
    public void CanReadMedicalRecord_AffectedFamilyMember_ReturnsTrue()
    {
        // Arrange
        var familyMemberId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.Role.Returns(nameof(UserRole.Adult));
        currentUser.FamilyMemberId.Returns(familyMemberId);
        var svc = new FamilyOsAuthorizationService(currentUser);
        var record = CreateMedicalRecord(familyMemberId);

        // Act & Assert
        svc.CanReadMedicalRecord(record).Should().BeTrue();
    }

    [Fact]
    public void CanReadMedicalRecord_OtherAdult_ReturnsFalse()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.Role.Returns(nameof(UserRole.Adult));
        currentUser.FamilyMemberId.Returns(Guid.NewGuid());
        var svc = new FamilyOsAuthorizationService(currentUser);
        var record = CreateMedicalRecord(Guid.NewGuid());

        // Act & Assert
        svc.CanReadMedicalRecord(record).Should().BeFalse();
    }
}
