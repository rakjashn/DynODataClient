# DynOData.Client

DynOData.Client is a modern, lightweight, and extensible .NET client library for interacting with OData v4 APIs. Designed with dependency injection and ease of use in mind, it provides a simple and robust way to perform CRUD operations, handle authentication, and execute bulk requests, with first-class support for Dynamics 365.

Why DynOData.Client?
Fluent & Simple Setup: Configure the client and its authentication with a single line of code.

Built for Dynamics 365: Tailored for the specific needs of Dynamics 365 Web API, including OAuth token management.

Pluggable Authentication: Out-of-the-box support for OAuth 2.0 (Client Credentials) and Basic Authentication.

Full OData v4 Support: Handles all standard operations, entity associations (Associate/Disassociate), and complex $batch requests.

Robust & Stateless: A thread-safe client designed for modern, high-concurrency applications.

Extensible by Design: Easily add your own DelegatingHandler for custom logging, caching, or other behaviors.

Installation
Install the package from NuGet using the .NET CLI:

dotnet add package DynOData.Client

(Note: This package name is a suggestion and may not exist yet.)

## Getting Started

### 1. Configure appsettings.json

Add the required configuration for your chosen authentication method.

For Dynamics 365 (OAuth):

```json
{
  "ODataOptions": {
    "BaseUrl": "[https://your-org.api.crm.dynamics.com/](https://your-org.api.crm.dynamics.com/)",
    "ODataUrlSuffix": "api/data/v9.2/",
    "TenantID": "your-azure-tenant-id",
    "ClientID": "your-application-client-id",
    "ClientSecret": "your-client-secret",
    "ScopeUrl": "[https://your-org.api.crm.dynamics.com/.default](https://your-org.api.crm.dynamics.com/.default)"
  }
}
```

### 2. Register the Client in Program.cs

Add the client to your service collection.

```c#
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add the OData Client for Dynamics 365
builder.Services.AddODataClientWithDynamicsAuth(builder.Configuration);

// Or, for Basic Auth:
// builder.Services.AddODataClientWithBasicAuth(builder.Configuration);

var app = builder.Build();
// ...
```

## Usage Examples

### 1. Inject the Client

Inject ODataClient into any service or controller via its constructor.

```c#
public class AccountService
{
    private readonly ODataClient _client;
    public AccountService(ODataClient client)
    {
        _client = client;
    }
    // ...
}
```

### 2. Reading Data

```c#
// Get a single entity by its key
string path = "accounts(00000000-0000-0000-0000-000000000001)?$select=name,accountnumber";
var account = await _client.GetAsync<Account>(path);

// Get a collection of entities with pagination
string path = "contacts?$select=fullname,emailaddress1";
var contacts = await _client.GetWithPaginationAsync<ODataCollection<Contact>>(path, pageSize: 50);
```

### 3. Creating and Updating Data

```c#
// Create a new entity
var newContact = new { firstname = "John", lastname = "Doe" };
await _client.PostAsync<object, object>("contacts", newContact);

// Create and return the new entity in one call
var newAccount = new { name = "Contoso Corp" };
var createdAccount = await _client.PostAndReturnRepresentationAsync<object, Account>("accounts", newAccount);

// Update an existing entity
var accountUpdate = new { telephone1 = "555-1234" };
await _client.PatchAsync<object, object>("accounts(00000000-0000-0000-0000-000000000001)", accountUpdate);
```

### 4. Deleting Data

```c#
await _client.DeleteAsync("contacts(00000000-0000-0000-0000-000000000002)");
```

### 5. Managing Relationships

```c#
// Associate a contact with an account (1-to-N)
string parentPath = "accounts(00000000-0000-0000-0000-000000000001)";
string relatedPath = $"{_client.BaseAddress}api/data/v9.2/contacts(00000000-0000-0000-0000-000000000002)";
await _client.AssociateAsync(parentPath, "contact_customer_accounts", relatedPath);

// Disassociate from a collection-valued property (1-to-N)
await _client.DisassociateAsync(parentPath, "contact_customer_accounts", "00000000-0000-0000-0000-000000000002");

// Disassociate from a single-valued property (N-to-1)
string orderPath = "salesorders(00000000-0000-0000-0000-000000000003)";
await _client.DisassociateAsync(orderPath, "customerid_account");
```

### 6. Bulk Operations ($batch)

```c#
using (var batchContent = new MultipartContent("mixed", $"batch_{Guid.NewGuid()}"))
{
    // 1. Create a new contact
    var newContact = new { firstname = "Jane", lastname = "Smith" };
    batchContent.Add(_client.PostBulkMessageContent("contacts", newContact, contentId: 1));

    // 2. Update an existing account
    var accountUpdate = new { telephone1 = "555-5678" };
    batchContent.Add(_client.PatchBulkMessageContent("accounts(00000000-0000-0000-0000-000000000001)", accountUpdate, contentId: 2));

    // 3. Delete another contact
    batchContent.Add(_client.DeleteBulkMessageContent("contacts(00000000-0000-0000-0000-000000000002)", contentId: 3));

    // Send the entire batch in one request
    var batchResponses = await _client.SendBulkRequestAsync(batchContent);

    // Process responses...
}
```

## Error Handling

The client throws a custom ODataClientException for non-successful HTTP status codes. You can use this to gracefully handle API errors.

```c#
try
{
    var account = await _client.GetAsync<Account>("accounts(non-existent-guid)");
}
catch (ODataClientException ex)
{
    Console.WriteLine($"API Error!");
    Console.WriteLine($"Status Code: {ex.StatusCode}"); // e.g., HttpStatusCode.NotFound
    Console.WriteLine($"Response: {ex.ResponseContent}"); // The raw JSON error from the API
}
```

## Contributing

Contributions are welcome! If you find a bug or have a feature request, please open an issue. If you want to contribute code, please open a pull request.

## License

This project is licensed under the MIT License. See the LICENSE file for details.
[MIT](https://choosealicense.com/licenses/mit/)
