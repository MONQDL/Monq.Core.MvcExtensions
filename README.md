# Monq.Core.MvcExtensions

Библиотека содержит набор расширений, который применяется в проекта AspNet Core для расширения поддержки REST интерфейсов.

### Установка

```powershell
Install-Package Monq.Core.MvcExtensions
```

### Фильтры

#### [ArrayInput]
###### Применяется, если требуется распарсить параметр URI типа /api?array=1,2,3,5,6,4 в массив типа T[].

**Пример**: Входной запрос: "http://localhost:5005/api/test?jobs=1,2,5,3,6,2"

```csharp
[HttpGet("/api/test")]
[ArrayInput("jobs")]
public IActionResult FilterBuildByJobIds(long[] jobs, long dateStart, long dateEnd, [FromQuery]PagingModel paging)
{
    ...
}
```

#### [ValidateActionParameters]
###### Применяется, если требуется провести валидацию входных параметров метода контроллера, а также, при наличии модели типа `[FromBody]`, проверить ее на null, и провести валидацию модели по аннотациям.
Если не прошли валидацию апраметры запроса, то возвращается `BadRequestResult` с такой структурой:
```csharp
{
    "message": "Error in query parameters.",
    "queryFields": {
        "id": [
            "The field id must be between 1 and 2147483647."
        ]
    }
}
```

Если модель с атрибутом [FromBody] невалидна или null, то возвращается BadRequestResult с такой структурой:

```javascript
{
    "message": "Request body in empty."
}
```

```javascript
{
    "message": "Wrong data model in request body.",
    "bodyFields": {
        "name": [
            "Name is required."
        ]
    }
}
```

**Пример**
```csharp
[HttpGet("/api/test/{id}")]
[ValidateActionParameters]
public IActionResult FilterBuildByJobIds([Range(1, int.MaxSize)]int id, [FromBody]ViewModel value)
{
    ...
}
```

#### [FilteredByAttribute]
###### Применяется, если требуется провести фильтрация по заданной модели.

**Пример**
```csharp
public class Userspace
{
    public long Id { get; set; };
    public WorkGroup { get; set; };
}

public class WorkGroup
{
    public long Id { get; set; };
}

public class UserspaceFilterViewModel
{
    [FilteredBy(nameof(Userspace.Id))]
    public List<long> Ids { get; set; } = null;

    [FilteredBy(nameof(Userspace.WorkGroup), nameof(WorkGroup.Id))]
    public List<long> WorkGroupIds { get; set; } = null;
}

public class UserspacesController : Controller
{
    [HttpPost(/api/userspaces/filter)]
    public IActionResult Filter([FromBody]UserspaceFilterViewModel value)
    {
    	var fmNamespaces = _context
    		.Userspaces
    		.FilterBy(value)
    		.ToList();
        ...
    }
}
```
#### [Computed]
###### Применяется, если требуется выполнить фильтрацию или сортировку по вычисляемому полю с его правильной конвертацией в SQL.
**Пример**
```csharp
   [Computed]
   public long Duration 
    => (EndClock.HasValue ? EndClock.Value : DateTimeOffset.Now.ToUnixTimeSeconds()) - (StartClock.HasValue ? StartClock.Value : DateTimeOffset.Now.ToUnixTimeSeconds());
```
### Расширения для работы с фильтром

#### Object.IsEmpty()

Возвращает `True` если все свойства объекта `null` или пустые.

**Пример**
```csharp
 if (value.IsEmpty())
   return BadRequest(new ErrorResponseModel("Пустой фильтр. Для получения списка пространств используйте GET /api/userspaces"));
```
