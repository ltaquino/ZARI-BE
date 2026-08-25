namespace ZARI.Application.Features.SystemModule.Forms.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllFormsQuery : IQuery<Result<List<FormResponse>>>;

public sealed record FormResponse(string Code, string Name, string Module);
