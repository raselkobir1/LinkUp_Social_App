export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
  statusCode: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  pageNumber: number;
  pageSize: number;
}
