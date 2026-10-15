using Microsoft.AspNetCore.Mvc.ModelBinding;

public sealed class EnumNameModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.ModelType;
        var enumType = Nullable.GetUnderlyingType(modelType) ?? modelType;

        return enumType.IsEnum
            ? new EnumNameModelBinder(enumType, Nullable.GetUnderlyingType(modelType) is not null)
            : null;
    }
}

public sealed class EnumNameModelBinder : IModelBinder
{
    private readonly Type _enumType;
    private readonly bool _isNullable;

    public EnumNameModelBinder(Type enumType, bool isNullable)
    {
        _enumType = enumType;
        _isNullable = isNullable;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

        var rawValue = valueResult.FirstValue?.Trim();
        if (string.IsNullOrEmpty(rawValue))
        {
            if (_isNullable)
                bindingContext.Result = ModelBindingResult.Success(null);
            else
                AddInvalidValueError(bindingContext, rawValue ?? string.Empty);

            return Task.CompletedTask;
        }

        if (IsNumeric(rawValue)
            || !Enum.TryParse(_enumType, rawValue, ignoreCase: true, out var parsedValue)
            || parsedValue is null
            || !Enum.IsDefined(_enumType, parsedValue))
        {
            AddInvalidValueError(bindingContext, rawValue);
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(parsedValue);
        return Task.CompletedTask;
    }

    private static bool IsNumeric(string value)
    {
        var start = value[0] is '+' or '-' ? 1 : 0;
        if (start == value.Length)
            return false;

        for (var index = start; index < value.Length; index++)
        {
            if (!char.IsDigit(value[index]))
                return false;
        }

        return true;
    }

    private static void AddInvalidValueError(ModelBindingContext context, string value)
    {
        context.ModelState.TryAddModelError(
            context.ModelName,
            $"O valor '{value}' não é válido para {context.FieldName}. Use o nome do enum.");
    }
}
