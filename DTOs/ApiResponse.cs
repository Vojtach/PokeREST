using System.Text.Json.Serialization;

namespace PokeREST.DTOs;

/// <summary>
/// Generic API response wrapper providing consistent response format with success status, message, and data.
/// </summary>
/// <remarks>
/// Every endpoint returns its payload wrapped in this envelope, except request-body validation failures, which
/// <c>[ApiController]</c> answers with a standard ProblemDetails 400 before the action runs. On success <see cref="Data"/> holds the result;
/// on failure <see cref="Data"/> is <c>null</c> and <see cref="Message"/> explains the error.
/// Construct instances through <see cref="SuccessResponse"/> and <see cref="ErrorResponse"/> rather than directly.
/// </remarks>
/// <typeparam name="T">The type of data being returned in the response.</typeparam>
/// <example>
/// Successful response from <c>GET /api/trainers/1</c>:
/// <code>
/// {
///   "success": true,
///   "statusCode": 200,
///   "message": "Trainer found successfully",
///   "data": { "id": 1, "name": "Ash Ketchum", ... }
/// }
/// </code>
/// Error response:
/// <code>
/// {
///   "success": false,
///   "statusCode": 404,
///   "message": "Trainer not found",
///   "data": null
/// }
/// </code>
/// </example>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was processed successfully.
    /// </summary>
    /// <remarks><c>true</c> for 2xx responses, <c>false</c> for client (4xx) and server (5xx) errors.</remarks>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// HTTP status code of the response, duplicated in the body for clients that cannot easily read headers.
    /// </summary>
    /// <example>200, 201, 400, 404, 500</example>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>
    /// Human-readable description of the outcome, suitable for logging or displaying to the user.
    /// </summary>
    /// <remarks>Never <c>null</c>; defaults to an empty string.</remarks>
    /// <example>"Found 3 Pokemon matching search criteria"</example>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The response payload. Populated on success; <c>null</c> (the default of <typeparamref name="T"/>) on error.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Creates a successful response wrapping the given data.
    /// </summary>
    /// <param name="data">The payload to return to the client.</param>
    /// <param name="message">Optional human-readable message describing the result.</param>
    /// <param name="statusCode">HTTP status code to report; defaults to 200 (use 201 for created resources).</param>
    /// <returns>A response with <see cref="Success"/> set to <c>true</c>.</returns>
    /// <example>
    /// <code>
    /// return Ok(ApiResponse&lt;TrainerResponseDto&gt;.SuccessResponse(trainer, "Trainer created successfully", 201));
    /// </code>
    /// </example>
    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation completed successfully", int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = statusCode,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Creates an error response with no data payload.
    /// </summary>
    /// <param name="message">Human-readable description of the error.</param>
    /// <param name="statusCode">HTTP status code to report; defaults to 400 (Bad Request).</param>
    /// <returns>A response with <see cref="Success"/> set to <c>false</c> and <see cref="Data"/> set to its default value.</returns>
    /// <example>
    /// <code>
    /// return NotFound(ApiResponse&lt;object&gt;.ErrorResponse("Trainer not found", 404));
    /// </code>
    /// </example>
    public static ApiResponse<T> ErrorResponse(string message, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Data = default
        };
    }
}

/// <summary>
/// Response wrapper for paginated data containing items and pagination metadata.
/// </summary>
/// <remarks>
/// Returned (inside an <see cref="ApiResponse{T}"/>) by the <c>/paginated</c> endpoints.
/// Page numbers are 1-based. Requesting a page beyond <see cref="TotalPages"/> yields an empty <see cref="Data"/> list.
/// </remarks>
/// <typeparam name="T">The type of items in the paginated collection.</typeparam>
/// <example>
/// Second page of 10 items out of 25 total:
/// <code>
/// {
///   "data": [ ... 10 items ... ],
///   "totalItems": 25,
///   "pageNumber": 2,
///   "pageSize": 10,
///   "totalPages": 3,
///   "hasPreviousPage": true,
///   "hasNextPage": true
/// }
/// </code>
/// </example>
public class PaginatedResponse<T>
{
    /// <summary>
    /// The items on the current page, in the requested sort order. Contains at most <see cref="PageSize"/> items.
    /// </summary>
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    /// <summary>
    /// Total number of items matching the filters across all pages.
    /// </summary>
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    /// <summary>
    /// The 1-based page number that was returned (echoes the request).
    /// </summary>
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    /// <summary>
    /// Maximum number of items per page (echoes the request; the intended 1-100 range is not enforced).
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages, calculated as <c>ceil(TotalItems / PageSize)</c>. Zero when no items match.
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    /// <summary>
    /// <c>true</c> when <see cref="PageNumber"/> is greater than 1.
    /// </summary>
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// <c>true</c> when <see cref="PageNumber"/> is less than <see cref="TotalPages"/>.
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }
}
