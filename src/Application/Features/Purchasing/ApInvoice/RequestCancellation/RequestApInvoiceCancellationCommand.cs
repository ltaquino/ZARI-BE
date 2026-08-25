namespace ZARI.Application.Features.Purchasing.ApInvoices.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record RequestApInvoiceCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<ApInvoiceResponse>>;
