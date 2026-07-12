using Microsoft.EntityFrameworkCore;
using StackUnderflow.Models;

namespace StackUnderflow.Extensions;

public static class PaginationExtensions
{
    public static async Task<PaginatedResponse<T>> ToPaginatedResponseAsync<T>(
        this IQueryable<T> query,
        PaginationRequest pagination)
    {
        var totalCount = await query.CountAsync();
        var data = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PaginatedResponse<T>(data, totalCount, pagination.Page, pagination.PageSize);
    }

    public static async Task<PaginatedResponse<T>> ToPaginatedResponseAsync<T>(
        this IQueryable<T> query,
        int page = 1,
        int pageSize = 20)
    {
        var pagination = new PaginationRequest { Page = page, PageSize = pageSize };
        return await query.ToPaginatedResponseAsync(pagination);
    }
}