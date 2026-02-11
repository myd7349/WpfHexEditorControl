# WPFHexaEditor.Tests - Unit Test Suite

Unit test project for the WPF HexEditor service layer using xUnit framework.

## 🧪 Test Framework

- **Framework**: xUnit 2.6.6
- **Target**: .NET 8.0-windows
- **Test Runner**: Microsoft.NET.Test.Sdk 17.8.0
- **Coverage Tool**: coverlet.collector 6.0.0

## 📊 Test Coverage

### Current Test Suites (80+ tests)

#### 1. **SelectionServiceTests** (35 tests)
Tests for selection validation, manipulation, and byte retrieval operations.

**Test Categories:**
- Selection validation (`IsValidSelection`, `FixSelectionRange`)
- Selection length calculations
- Byte retrieval (`GetSelectionBytes`, `GetAllBytes`)
- Selection extension and boundary checks
- Select all operations

#### 2. **FindReplaceServiceTests** (35 tests)
Tests for search, replace, and caching functionality.

**Test Categories:**
- Find operations (`FindFirst`, `FindNext`, `FindLast`, `FindAll`)
- Cached search with timeout
- Replace operations (`ReplaceByte`, `ReplaceFirst`, `ReplaceNext`, `ReplaceAll`)
- Cache management and invalidation
- Read-only mode behavior

#### 3. **HighlightServiceTests** (10+ tests)
Tests for stateful highlight management (search results, marked bytes).

**Test Categories:**
- Adding/removing highlights
- Highlight validation (`IsHighlighted`)
- Range grouping (`GetHighlightedRanges`)
- State persistence across operations
- Clear all highlights

## 🏃 Running Tests

### Run all tests:
```bash
dotnet test
```

### Run with detailed output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run specific test class:
```bash
dotnet test --filter "FullyQualifiedName~SelectionServiceTests"
```

### Run with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📁 Project Structure

```
WPFHexaEditor.Tests/
├── Services/
│   ├── SelectionServiceTests.cs      (35 tests)
│   ├── FindReplaceServiceTests.cs     (35 tests)
│   └── HighlightServiceTests.cs       (10+ tests)
└── WPFHexaEditor.Tests.csproj
```

## 🎯 Test Patterns Used

### 1. **Arrange-Act-Assert Pattern**
All tests follow the AAA pattern for clarity:
```csharp
[Fact]
public void Method_Scenario_ExpectedBehavior()
{
    // Arrange
    var service = new Service();

    // Act
    var result = service.Method(input);

    // Assert
    Assert.Equal(expected, result);
}
```

### 2. **Test Data Setup**
- Uses `IDisposable` pattern for cleanup
- Creates temporary files for ByteProvider tests
- Proper resource disposal after each test

### 3. **Comprehensive Coverage**
- Happy path scenarios
- Edge cases (null, empty, out-of-bounds)
- Error conditions
- State persistence

## 🚧 Future Test Additions

Tests planned for remaining 7 services:
- ByteModificationServiceTests
- ClipboardServiceTests
- UndoRedoServiceTests
- BookmarkServiceTests
- CustomBackgroundServiceTests
- PositionServiceTests
- TblServiceTests

## 📝 Test Naming Convention

```
MethodName_StateUnderTest_ExpectedBehavior
```

**Examples:**
- `FindFirst_ExistingPattern_ReturnsFirstPosition`
- `GetSelectionBytes_NullProvider_ReturnsNull`
- `AddHighLight_ValidRange_AddsHighlights`

## 🔍 Testing Best Practices

1. **Isolated Tests**: Each test is independent and can run in any order
2. **No External Dependencies**: Tests use in-memory data or temporary files
3. **Fast Execution**: All tests complete in seconds
4. **Deterministic**: Tests produce consistent results
5. **Single Responsibility**: Each test verifies one behavior

## 📚 Related Documentation

- [Services Documentation](../WPFHexaEditor/Services/README.md) - Service layer architecture
- [Main README](../../README.md) - Project overview
- [xUnit Documentation](https://xunit.net/) - Test framework docs

## 📊 Test Metrics

- **Total Tests**: 80+
- **Pass Rate**: Target 100%
- **Execution Time**: < 10 seconds
- **Service Coverage**: 3/10 services (30%)

## 🛠️ Development

### Adding New Tests

1. Create test file in `Services/` folder
2. Follow naming convention: `[ServiceName]Tests.cs`
3. Use `IDisposable` for setup/cleanup if needed
4. Group tests with `#region` directives
5. Add copyright header (2016-2026)

### Example Test Structure

```csharp
//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using Xunit;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Tests.Services
{
    public class ServiceNameTests : IDisposable
    {
        private readonly ServiceName _service;

        public ServiceNameTests()
        {
            _service = new ServiceName();
        }

        public void Dispose()
        {
            // Cleanup
        }

        #region Test Category

        [Fact]
        public void Method_Scenario_Behavior()
        {
            // Test implementation
        }

        #endregion
    }
}
```

---

✨ Built with xUnit and .NET 8.0-windows
