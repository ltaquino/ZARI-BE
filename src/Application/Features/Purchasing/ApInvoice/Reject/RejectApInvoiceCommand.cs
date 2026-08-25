namespace ZARI.Application.Features.Purchasing.ApInvoices.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record RejectApInvoiceCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<ApInvoiceResponse>>;
