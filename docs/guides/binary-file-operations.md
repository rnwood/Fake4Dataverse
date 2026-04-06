# Binary & File Operations

Fake4Dataverse supports storing and retrieving binary data (images and files) for entity attributes. Both direct helper methods and the block-based upload/download requests used by the Dataverse SDK are supported.

---

## Direct Binary Storage

The simplest approach uses the helper methods on `FakeOrganizationService`:

```csharp
var service = new FakeOrganizationService();
var accountId = service.Create(new Entity("account") { ["name"] = "Contoso" });

// Store a binary attribute (e.g., an entity image)
byte[] imageData = File.ReadAllBytes("logo.png");
service.SetBinaryAttribute("account", accountId, "entityimage", imageData);

// Retrieve the binary attribute
byte[]? retrieved = service.GetBinaryAttribute("account", accountId, "entityimage");

Assert.NotNull(retrieved);
Assert.Equal(imageData.Length, retrieved.Length);
```

- **`SetBinaryAttribute(entityName, entityId, attributeName, byte[] data)`** — stores a copy of the data.
- **`GetBinaryAttribute(entityName, entityId, attributeName)`** — returns a copy of the data, or `null` if not set.

Data is deep-cloned on both store and retrieve to prevent aliasing bugs.

---

## File Upload Session (Block-Based)

For larger files or when testing code that uses the Dataverse file upload protocol, use the block-based request handlers:

### 1. Initialize Upload

```csharp
var initResponse = (InitializeFileBlocksUploadResponse)service.Execute(
    new InitializeFileBlocksUploadRequest
    {
        Target = new EntityReference("annotation", annotationId),
        FileAttributeName = "documentbody"
    });

string token = initResponse.FileContinuationToken;
```

### 2. Upload Blocks

```csharp
service.Execute(new UploadBlockRequest
{
    FileContinuationToken = token,
    BlockData = chunkBytes,
    BlockId = Convert.ToBase64String(BitConverter.GetBytes(blockIndex))
});
```

You can upload multiple blocks sequentially. Each block is stored in order.

### 3. Commit Upload

```csharp
var commitResponse = (CommitFileBlocksUploadResponse)service.Execute(
    new CommitFileBlocksUploadRequest
    {
        FileContinuationToken = token,
        FileName = "document.pdf",
        MimeType = "application/pdf",
        BlockList = blockIds.ToArray()
    });
```

All uploaded blocks are assembled and stored as a single binary attribute.

### 4. Download

```csharp
var downloadResponse = (DownloadBlockResponse)service.Execute(
    new DownloadBlockRequest
    {
        FileContinuationToken = token
    });
```

### 5. Delete

```csharp
service.Execute(new OrganizationRequest("DeleteFile")
{
    ["FileAttributeName"] = "documentbody",
    ["Target"] = new EntityReference("annotation", annotationId)
});
```

---

## Complete Example

```csharp
var service = new FakeOrganizationService();
var id = service.Create(new Entity("account") { ["name"] = "Test" });

// Store binary data
var original = new byte[] { 0x01, 0x02, 0x03, 0x04 };
service.SetBinaryAttribute("account", id, "entityimage", original);

// Retrieve and verify
var result = service.GetBinaryAttribute("account", id, "entityimage");
Assert.Equal(original, result);

// Overwrite
var updated = new byte[] { 0xAA, 0xBB };
service.SetBinaryAttribute("account", id, "entityimage", updated);
result = service.GetBinaryAttribute("account", id, "entityimage");
Assert.Equal(updated, result);
```

---

## Tips

- **Deep cloning** — binary data is always cloned on store and retrieve, so modifying the returned array does not affect stored data.
- **Direct vs. block-based** — use direct methods for simple test setups; use block-based requests when testing code that relies on the SDK file upload protocol.
- **No size limits** — the fake has no file size restrictions, unlike Dataverse which enforces per-attribute limits.
