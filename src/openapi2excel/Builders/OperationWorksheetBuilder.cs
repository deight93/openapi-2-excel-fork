﻿using ClosedXML.Excel;
using Microsoft.OpenApi.Models;
using OpenApi2Excel.Builders.WorksheetPartsBuilders;

namespace OpenApi2Excel.Builders;

internal class OperationWorksheetBuilder(IXLWorkbook workbook, OpenApiDocumentationOptions options)
    : WorksheetBuilder(options)
{
    private readonly RowPointer _actualRowPointer = new(1);
    private IXLWorksheet _worksheet = null!;
    private int _attributesColumnsStartIndex;

    public IXLWorksheet Build(string path, OpenApiPathItem pathItem, OperationType operationType, OpenApiOperation operation)
    {
        _worksheet = workbook.Worksheets.Add(operation.OperationId);
        _actualRowPointer.GoTo(1);

        SetMaxTreeLevel(operation);
        AdjustColumnsWidthToRequestTreeLevel();

        AddHomePageLink();
        AddOperationInfos(path, pathItem, operationType, operation);
        AddRequestParameters(operation);
        AddRequestBody(operation);
        AdjustLastNamesColumnToContents();

        return _worksheet;
    }

    private void SetMaxTreeLevel(OpenApiOperation operation)
    {
        if (operation.RequestBody is null)
            return;

        _attributesColumnsStartIndex = operation.RequestBody.Content
            .Select(openApiMediaType => openApiMediaType.Value.Schema)
            .Select(schema => EstablishMaxTreeLevel(schema, 0))
            .Prepend(1)
            .Max();
    }

    private void AdjustColumnsWidthToRequestTreeLevel()
    {
        for (var columnIndex = 1; columnIndex < _attributesColumnsStartIndex - 1; columnIndex++)
        {
            _worksheet.Column(columnIndex).Width = 2;
        }
    }

    private void AdjustLastNamesColumnToContents()
    {
        _worksheet.Column(_attributesColumnsStartIndex - 1).AdjustToContents();
    }

    private static int EstablishMaxTreeLevel(OpenApiSchema schema, int currentLevel)
    {
        currentLevel++;
        return schema.Properties?.Any() != true
            ? currentLevel
            : schema.Properties
                .Select(schemaProperty => EstablishMaxTreeLevel(schemaProperty.Value, currentLevel))
                .Prepend(currentLevel)
                .Max();
    }

    private void AddOperationInfos(string path, OpenApiPathItem pathItem, OperationType operationType, OpenApiOperation operation) =>
        new OperationInfoBuilder(_actualRowPointer, _attributesColumnsStartIndex, _worksheet, Options)
            .AddOperationInfoPart(path, pathItem, operationType, operation);

    private void AddRequestParameters(OpenApiOperation operation) =>
        new RequestParametersBuilder(_actualRowPointer, _attributesColumnsStartIndex, _worksheet, Options)
            .AddRequestParametersPart(operation);

    private void AddRequestBody(OpenApiOperation operation) =>
        new RequestBodyBuilder(_actualRowPointer, _attributesColumnsStartIndex, _worksheet, Options)
            .AddRequestBodyPart(operation);

    private void AddHomePageLink() => new HomePageLinkBuilder(_actualRowPointer, _worksheet, Options)
        .AddHomePageLinkPart();
}