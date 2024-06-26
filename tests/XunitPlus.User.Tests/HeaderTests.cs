using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace XunitPlus.User.Tests;

[Authorization("Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IkQ4NjdGNzEwMEM1OENDRDFBNUUzMzVFNEEzN0RGNTUwIiwidHlwIjoiSldUIn0.eyJuYmYiOjE3MDQzNTAxNTIsImV4cCI6MTcwNDQzNjU1MiwiaXNzIjoiaHR0cDovL3d3dy5oeXN5eWwuY29tIiwiYXVkIjpbIkh5c01hbGwuQmFzZUJ1c2luZXNzLkFQSSIsIkh5c01hbGwuQ2xpZW50LkFQSSIsIkh5c01hbGwuU3lzTWFuYWdlbWVudC5BUEkiLCJaSlMuSU0uQ2xpZW50LkFwcEFwaSIsInpqcy5vc3MuYXBpIl0sImNsaWVudF9pZCI6Ikh5c01hbGwiLCJzdWIiOiI2OTAwMDAwMDAwMDAwMDAwMDAwIiwiYXV0aF90aW1lIjoxNzA0MzUwMTUyLCJpZHAiOiJsb2NhbCIsIm5hbWUiOiLkvZXov5zliKkiLCJuaWNrbmFtZSI6IiIsInJvbGUiOiJBZG1pbmlzdHJhdG9yIiwidGltZXN0YW1wIjoiNjM4Mzk5NzU3NTI5MjYxNTQ4IiwibG9naW5pZCI6Ijk5MjY1MjFDLTNFRDctNEJDNC05MEM5LTA1NERDODJGRUExQiIsImJ1c2luZXNzSWQiOiI2OTcwMjAyMTA0OTc1NzIwNDQ4IiwianRpIjoiQ0M1NkQ5QzMyNEEwREZFOTUxRERDRDRBODk0QzQ1NkQiLCJpYXQiOjE3MDQzNTAxNTIsInNjb3BlIjoiSHlzTWFsbC5CYXNlQnVzaW5lc3MuQVBJIEh5c01hbGwuQ2xpZW50LkFQSSBIeXNNYWxsLlN5c01hbmFnZW1lbnQuQVBJIG9wZW5pZCBwcm9maWxlIFpKUy5JTS5DbGllbnQuQXBwQXBpIHpqcy5vc3MuYXBpIG9mZmxpbmVfYWNjZXNzIiwiYW1yIjpbImN1c3RvbSJdfQ.ehd_2Yu6SqZ0FckZKcdNaEthsv61iJL2GTQvSuR8aROPHlNAUVgfw9Yhj_r3f8AxVBrU7poIorIYHmKEp07TkoRtq5vHT25-2NhrWCV3reR3pqbcWYTZUyG4KS9AdYMCWKcqhUFyOjpjuAIWWUDWJVHKvh9MkJnFT5-1WYEFAE3e32WFtbOskZf5fAQozpn0dn8HHydU-k5yKhWs3QXGGiG5od_Tc88q-f31irs4-ot6gZI_BgVu4oAOB4wJGhcro0p9ygQGMcVo4_BMWSaBIf4DiJ6AZkS6LCBs-Z8PcGnfq72iqMGXJokAdqd0h9j5C5MBr4FqgNjFqCwM4Kd4rw")]
public class HeaderTests(IHttpContextAccessor accessor)
{
    [Fact]
    public void Test()
    {
        Debug.WriteLine(accessor!.HttpContext!.Request.Headers.Authorization);
    }
}