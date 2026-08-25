namespace ZARI.Application.Features.Purchasing.ApInvoices.RejectCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record RejectApInvoiceCancellationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<ApInvoiceResponse>>;
