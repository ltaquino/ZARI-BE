namespace ZARI.Application.Features.Purchasing.ApInvoices.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveApInvoiceCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<ApInvoiceResponse>>;
