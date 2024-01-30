﻿using AutoMapper;
using HCQS.BackEnd.Common.Dto;
using HCQS.BackEnd.Common.Dto.BaseRequest;
using HCQS.BackEnd.Common.Dto.Record;
using HCQS.BackEnd.Common.Dto.Request;
using HCQS.BackEnd.Common.Util;
using HCQS.BackEnd.DAL.Contracts;
using HCQS.BackEnd.DAL.Implementations;
using HCQS.BackEnd.DAL.Models;
using HCQS.BackEnd.Service.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Globalization;
using System.Transactions;

namespace HCQS.BackEnd.Service.Implementations
{
    public class ExportPriceMaterialService : GenericBackendService, IExportPriceMaterialService
    {
        private IExportPriceMaterialRepository _exportPriceMaterialRepository;
        private IMapper _mapper;
        private BackEndLogger _logger;
        private IUnitOfWork _unitOfWork;
        private IFileService _fileService;

        public ExportPriceMaterialService(IExportPriceMaterialRepository exportPriceMaterialRepository, IMapper mapper, BackEndLogger logger, IUnitOfWork unitOfWork, IFileService fileService, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _exportPriceMaterialRepository = exportPriceMaterialRepository;
            _mapper = mapper;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        public async Task<AppActionResult> CreateExportPriceMaterial(ExportPriceMaterialRequest ExportPriceMaterialRequest)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                AppActionResult result = new AppActionResult();
                try
                {
                    if (ExportPriceMaterialRequest.Price <= 0)
                    {
                        result = BuildAppActionResultError(result, $"Inputted price must be greater than 0");
                    }
                    else
                    {
                        var materialRepository = Resolve<IMaterialRepository>();
                        var materialDb = await materialRepository.GetByExpression(n => n.Id.Equals(ExportPriceMaterialRequest.MaterialId));
                        if (materialDb == null)
                        {
                            result = BuildAppActionResultError(result, $"The material with id is {ExportPriceMaterialRequest.MaterialId} does not exist!");
                        }
                        else
                        {
                            var exportPriceMaterial = _mapper.Map<ExportPriceMaterial>(ExportPriceMaterialRequest);
                            exportPriceMaterial.Id = Guid.NewGuid();
                            if (exportPriceMaterial.Date == null)
                            {
                                exportPriceMaterial.Date = DateTime.Now;
                            }
                            result.Result.Data = await _exportPriceMaterialRepository.Insert(exportPriceMaterial);
                            await _unitOfWork.SaveChangeAsync();
                        }
                    }

                    if (!BuildAppActionResultIsError(result))
                    {
                        scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    result = BuildAppActionResultError(result, SD.ResponseMessage.INTERNAL_SERVER_ERROR, true);
                    _logger.LogError(ex.Message, this);
                }
                return result;
            }
        }

        public async Task<AppActionResult> DeleteExportPriceMaterialById(Guid id)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                AppActionResult result = new AppActionResult();
                try
                {
                    var exportPriceMaterialDb = await _exportPriceMaterialRepository.GetById(id);
                    if (exportPriceMaterialDb == null)
                    {
                        result = BuildAppActionResultError(result, $"The supplier with {id} not found !");
                    }
                    else
                    {
                        result.Result.Data = await _exportPriceMaterialRepository.DeleteById(id);
                        await _unitOfWork.SaveChangeAsync();
                    }

                    if (!BuildAppActionResultIsError(result))
                    {
                        scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    result = BuildAppActionResultError(result, SD.ResponseMessage.INTERNAL_SERVER_ERROR, true);
                    _logger.LogError(ex.Message, this);
                }
                return result;
            }
        }

        public async Task<AppActionResult> GetAll(int pageIndex, int pageSize, IList<SortInfo> sortInfos)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                AppActionResult result = new AppActionResult();
                try
                {
                    var exportPriceMaterialList = await _exportPriceMaterialRepository.GetAllDataByExpression(null, null);

                    if (exportPriceMaterialList.Any())
                    {
                        if (pageIndex <= 0) pageIndex = 1;
                        if (pageSize <= 0) pageSize = SD.MAX_RECORD_PER_PAGE;
                        int totalPage = DataPresentationHelper.CalculateTotalPageSize(exportPriceMaterialList.Count(), pageSize);

                        if (sortInfos != null)
                        {
                            exportPriceMaterialList = DataPresentationHelper.ApplySorting(exportPriceMaterialList, sortInfos);
                        }
                        if (pageIndex > 0 && pageSize > 0)
                        {
                            exportPriceMaterialList = DataPresentationHelper.ApplyPaging(exportPriceMaterialList, pageIndex, pageSize);
                        }
                        result.Result.Data = exportPriceMaterialList;
                        result.Result.TotalPage = totalPage;
                    }
                    else
                    {
                        result.Messages.Add("Empty export price list");
                    }

                    if (!BuildAppActionResultIsError(result))
                    {
                        scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    result = BuildAppActionResultError(result, SD.ResponseMessage.INTERNAL_SERVER_ERROR, true);
                    _logger.LogError(ex.Message, this);
                }
                return result;
            }
        }

        public async Task<AppActionResult> GetExportPriceMaterialById(Guid id)
        {
            AppActionResult result = new AppActionResult();
            try
            {
                var exportPriceMaterialDb = await _exportPriceMaterialRepository.GetById(id);
                if (exportPriceMaterialDb != null)
                {
                    result.Result.Data = exportPriceMaterialDb;
                }
            }
            catch (Exception ex)
            {
                result = BuildAppActionResultError(result, SD.ResponseMessage.INTERNAL_SERVER_ERROR, true);
                _logger.LogError(ex.Message, this);
            }
            return result;
        }

        public async Task<AppActionResult> GetLatestPrice(int pageIndex, int pageSize, IList<SortInfo> sortInfos)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                AppActionResult result = new AppActionResult();
                try
                {
                    var exportPriceMaterialList = await _exportPriceMaterialRepository.GetAllDataByExpression(null, null);
                    var latestPriceMaterialList = exportPriceMaterialList
                                                .GroupBy(x => x.MaterialId)
                                                .Select(g => g.OrderByDescending(x => x.Date).FirstOrDefault()).ToList();
                    if (latestPriceMaterialList.Any())
                    {
                        if (pageIndex <= 0) pageIndex = 1;
                        if (pageSize <= 0) pageSize = SD.MAX_RECORD_PER_PAGE;
                        int totalPage = DataPresentationHelper.CalculateTotalPageSize(latestPriceMaterialList.Count(), pageSize);

                        if (sortInfos != null)
                        {
                            latestPriceMaterialList = DataPresentationHelper.ApplySorting(latestPriceMaterialList, sortInfos);
                        }
                        if (pageIndex > 0 && pageSize > 0)
                        {
                            latestPriceMaterialList = DataPresentationHelper.ApplyPaging(latestPriceMaterialList, pageIndex, pageSize);
                        }
                        result.Result.Data = latestPriceMaterialList;
                        result.Result.TotalPage = totalPage;
                    }
                    else
                    {
                        result.Messages.Add("Empty export price list");
                    }

                    if (!BuildAppActionResultIsError(result))
                    {
                        scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    result = BuildAppActionResultError(result, SD.ResponseMessage.INTERNAL_SERVER_ERROR, true);
                    _logger.LogError(ex.Message, this);
                }
                return result;
            }
        }

        public async Task<AppActionResult> UpdateExportPriceMaterial(ExportPriceMaterialRequest ExportPriceMaterialRequest)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                AppActionResult result = new AppActionResult();
                try
                {
                    if (ExportPriceMaterialRequest.Price <= 0)
                    {
                        result = BuildAppActionResultError(result, $"Inputted price must be greater than 0");
                    }
                    else
                    {
                        var exportPriceMaterialDb = await _exportPriceMaterialRepository.GetById(ExportPriceMaterialRequest.Id);
                        if (exportPriceMaterialDb == null)
                        {
                            result = BuildAppActionResultError(result, $"The export price material with {ExportPriceMaterialRequest.Id} not found !");
                        }
                        else
                        {
                            exportPriceMaterialDb.Price = ExportPriceMaterialRequest.Price;
                            if (ExportPriceMaterialRequest.Date != null)
                                exportPriceMaterialDb.Date = (DateTime)ExportPriceMaterialRequest.Date;
                            result.Result.Data = await _exportPriceMaterialRepository.Update(exportPriceMaterialDb);
                            await _unitOfWork.SaveChangeAsync();
                        }
                    }

                    if (!BuildAppActionResultIsError(result))
                    {
                        scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    result = BuildAppActionResultError(result, SD.ResponseMessage.INTERNAL_SERVER_ERROR, true);
                    _logger.LogError(ex.Message, this);
                }
                return result;
            }
        }

        public async Task<IActionResult> UploadExportPriceMaterialWithExcelFile(IFormFile file)
        {
            IActionResult result = null;
            if (file == null || file.Length == 0)
            {
                return result;
            }
            else
            {
                bool isSuccessful = true;
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    try
                    {
                        //Format: Name_ddmmyyy
                        string dateString = file.FileName.Substring(0, 8);
                        if (!DateTime.TryParseExact(dateString, "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                        {
                            isSuccessful = false;
                            _logger.LogError($"{dateString} is not in format: ddMMyyyy", this);
                        }
                        else
                        {
                            Dictionary<String, Guid> materials = new Dictionary<String, Guid>();
                            List<ExportPriceMaterialRecord> records = await GetListFromExcel(file);
                            List<ExportPriceMaterial> exportPriceMaterials = new List<ExportPriceMaterial>();
                            var materialRepository = Resolve<IMaterialRepository>();
                            List<int> invalidRowInput = new List<int>();
                            int i = 2;
                            foreach (ExportPriceMaterialRecord record in records)
                            {
                                Guid materialId = Guid.Empty;
                                if (materials.ContainsKey(record.MaterialName)) materialId = materials[record.MaterialName];
                                else
                                {
                                    var material = await materialRepository.GetByExpression(m => m.Name.Equals(record.MaterialName));
                                    if (material == null)
                                    {
                                        invalidRowInput.Add(i);
                                    }
                                    else
                                    {
                                        materialId = material.Id;
                                        materials.Add(record.MaterialName, materialId);
                                    }
                                }

                                if (invalidRowInput.Count == 0)
                                {
                                    var newPriceDetail = new ExportPriceMaterial()
                                    {
                                        Id = Guid.NewGuid(),
                                        MaterialId = materialId,
                                        Date = record.Date,
                                        Price = record.Price
                                    };
                                    await _exportPriceMaterialRepository.Insert(newPriceDetail);
                                    exportPriceMaterials.Add(newPriceDetail);
                                }
                                i++;
                            }

                            if (invalidRowInput.Count > 0)
                            {
                                List<List<string>> recordDataString = new List<List<string>>();
                                int j = 1;
                                foreach (var record in records)
                                {
                                    recordDataString.Add(new List<string>
                                        {
                                            j++.ToString(), record.MaterialName, record.Price.ToString(), record.Date.ToString()
                                        });
                                }
                                result = _fileService.ReturnErrorColored<ExportPriceMaterialRecord>(SD.ExcelHeaders.EXPORT_PRICE_DETAIL, recordDataString, invalidRowInput, dateString);
                                _logger.LogError($"Invalid rows are colored in the excel file!", this);
                                isSuccessful = false;
                            }

                            if (isSuccessful)
                            {
                                await _unitOfWork.SaveChangeAsync();
                                result = new ObjectResult(exportPriceMaterials) { StatusCode = 200 };
                            }
                        }
                        if (isSuccessful)
                        {
                            scope.Complete();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message, this);
                    }
                }
            }
            return result;
        }

        private async Task<List<ExportPriceMaterialRecord>> GetListFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using (ExcelPackage package = new ExcelPackage(stream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0]; // Assuming data is in the first sheet

                        int rowCount = worksheet.Dimension.Rows;
                        int colCount = worksheet.Dimension.Columns;

                        List<ExportPriceMaterialRecord> records = new List<ExportPriceMaterialRecord>();

                        for (int row = 2; row <= rowCount; row++) // Assuming header is in the first row
                        {
                            ExportPriceMaterialRecord record = new ExportPriceMaterialRecord()
                            {
                                Id = Guid.NewGuid(),
                                MaterialName = worksheet.Cells[row, 2].Value.ToString().ToString(),
                                Price = double.Parse(worksheet.Cells[row, 3].Value.ToString().ToString()),
                                Date = DateTime.Parse(worksheet.Cells[row, 4].Value.ToString().ToString())
                            };
                            records.Add(record);
                        }
                        return records;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, this);
            }
            return null;
        }

        public async Task<IActionResult> GetExportPriceMaterialTemplate()
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                IActionResult result = null;
                try
                {
                    List<ExportPriceMaterialRecord> sampleData = new List<ExportPriceMaterialRecord>();
                    sampleData.Add(new ExportPriceMaterialRecord
                    { MaterialName = "Brick", Price = 999, Date = DateTime.Parse("2024-01-24T14:30:00") });
                    result = _fileService.GenerateExcelContent<ExportPriceMaterialRecord>(sampleData, "ExportPriceTemplate");
                    if (result != null)
                    {
                        scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, this);
                }
                return result;
            }

        }
    }
}
