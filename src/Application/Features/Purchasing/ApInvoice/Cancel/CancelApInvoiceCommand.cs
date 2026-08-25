namespace ZARI.Application.Features.Purchasing.ApInvoices.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record CancelApInvoiceCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<ApInvoiceResponse>>;
