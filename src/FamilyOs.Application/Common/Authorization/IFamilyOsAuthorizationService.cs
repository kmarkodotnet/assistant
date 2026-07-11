using FamilyOs.Domain.Entities;

namespace FamilyOs.Application.Common.Authorization;

public interface IFamilyOsAuthorizationService
{
    bool CanReadDocument(Document document);
    bool CanWriteDocument(Document document);
    bool CanReadMedicalRecord(MedicalRecord record);
    bool CanReadTask(FamilyTask task);
    bool CanReadDeadline(Deadline deadline);
}
