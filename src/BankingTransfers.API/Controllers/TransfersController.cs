using System.ComponentModel.DataAnnotations;
using BankingTransfers.API.DTOs;
using BankingTransfers.Application.Common;
using BankingTransfers.Application.Transfers.Create;
using BankingTransfers.Application.Transfers.Get;
using BankingTransfers.Application.Transfers.Validate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BankingTransfers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransfersController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransfersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Validate(
        [FromHeader(Name = "UserProfileUID"), Required] Guid userProfileUId,
        [FromBody] ValidateTransferRequestDto dto,
        CancellationToken cancellationToken)
    {
        var request = new ValidateTransferRequest
        {
            UserProfileUId = userProfileUId,
            SourceIban = dto.SourceIban,
            TargetIban = dto.TargetIban,
            Amount = dto.Amount,
            Currency = dto.Currency,
            Reason = dto.Reason,
            ExecutionDate = dto.ExecutionDate
        };

        var result = await _mediator.Send(request, cancellationToken);

        return result.Status switch
        {
            ResultStatus.Success => Ok(new ValidationResponseDto { IsValid = true }),
            ResultStatus.NotFound => NotFound(new ValidationResponseDto { IsValid = false, Errors = result.Errors }),
            ResultStatus.ValidationFailure => BadRequest(new ValidationResponseDto { IsValid = false, Errors = result.Errors }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateTransferResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromHeader(Name = "UserProfileUID"), Required] Guid userProfileUId,
        [FromBody] CreateTransferRequestDto dto,
        CancellationToken cancellationToken)
    {
        var request = new CreateTransferRequest
        {
            UserProfileUId = userProfileUId,
            IdempotencyKey = dto.IdempotencyKey,
            SourceIban = dto.SourceIban,
            TargetIban = dto.TargetIban,
            Amount = dto.Amount,
            Currency = dto.Currency,
            Reason = dto.Reason,
            ExecutionDate = dto.ExecutionDate
        };

        var result = await _mediator.Send(request, cancellationToken);

        return result.Status switch
        {
            ResultStatus.Success => Ok(new CreateTransferResponseDto { UId = result.UId!.Value, Status = result.TransferStatus! }),
            ResultStatus.NotFound => NotFound(new ValidationResponseDto { IsValid = false, Errors = result.Errors }),
            ResultStatus.ValidationFailure => BadRequest(new ValidationResponseDto { IsValid = false, Errors = result.Errors }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("{transferUId:guid}")]
    [ProducesResponseType(typeof(GetTransferResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromHeader(Name = "UserProfileUID"), Required] Guid userProfileUId,
        Guid transferUId,
        CancellationToken cancellationToken)
    {
        var request = new GetTransferRequest
        {
            UserProfileUId = userProfileUId,
            TransferUId = transferUId
        };

        var result = await _mediator.Send(request, cancellationToken);

        return result.Status switch
        {
            ResultStatus.NotFound => NotFound(new ValidationResponseDto { IsValid = false, Errors = result.Errors }),
            ResultStatus.Success => Ok(new GetTransferResponseDto
            {
                UId = result.UId!.Value,
                SourceIban = result.SourceIban!,
                TargetIban = result.TargetIban!,
                Amount = result.Amount!.Value,
                Currency = result.Currency!,
                Reason = result.Reason!,
                ExecutionDate = result.ExecutionDate!.Value,
                Status = result.TransferStatus!,
                CreatedAt = result.CreatedAt!.Value,
                ProcessingStartedAt = result.ProcessingStartedAt,
                ProcessedAt = result.ProcessedAt,
                RetryCount = result.RetryCount!.Value,
                ErrorMessage = result.ErrorMessage
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
