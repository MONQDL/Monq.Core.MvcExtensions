# Библиотека поддержки микросервисов .net core

Библиотека содержит набор расширений, который применяется в проекта AspNet Core.

### Установка

```powershell
Install-Package Monq.Tools.MvcExtensions -Source http://nuget.monq.ru/nuget/Default
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

#### [ValidateModel]
###### Применяется, если требуется проверить входную модель на null, а также провести валидацию модели по аннотациям.
Если модель невалидна или null, то возвращается BadRequestResult с такой структурой.

**Пример**
```csharp
[HttpGet("/api/test")]
[ValidateModel("value")]
public IActionResult FilterBuildByJobIds([FromBody]ViewModel value)
{
    ...
}
```