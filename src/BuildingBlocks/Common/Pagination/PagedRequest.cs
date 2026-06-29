using LinkUp.SharedKernel.Constants;

namespace LinkUp.BuildingBlocks.Common.Pagination;

public class PagedRequest
{
    private int _pageSize = AppConstants.Pagination.DefaultPageSize;
    private int _pageNumber = AppConstants.Pagination.DefaultPage;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > AppConstants.Pagination.MaxPageSize
            ? AppConstants.Pagination.MaxPageSize
            : value < 1 ? 1 : value;
    }

    public int Skip => (PageNumber - 1) * PageSize;
}
