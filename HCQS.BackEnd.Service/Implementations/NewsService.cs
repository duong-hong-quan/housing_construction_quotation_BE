﻿using AutoMapper;
using HCQS.BackEnd.Common.Dto;
using HCQS.BackEnd.Common.Dto.BaseRequest;
using HCQS.BackEnd.Common.Dto.Request;
using HCQS.BackEnd.Common.Util;
using HCQS.BackEnd.DAL.Contracts;
using HCQS.BackEnd.DAL.Models;
using HCQS.BackEnd.Service.Contracts;
using System.Transactions;

namespace HCQS.BackEnd.Service.Implementations
{
    public class NewsService : GenericBackendService, INewsService
    {
        private BackEndLogger _logger;
        private IUnitOfWork _unitOfWork;
        private IMapper _mapper;
        private INewsRepository _newsRepository;

        public NewsService(BackEndLogger backEndLogger, IUnitOfWork unitOfWork, IMapper mapper, INewsRepository newsRepository, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = backEndLogger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _newsRepository = newsRepository;
        }

        public async Task<AppActionResult> CreateNews(NewsRequest NewsRequest)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                AppActionResult result = new AppActionResult();
                try
                {
                    var news = _mapper.Map<News>(NewsRequest);
                    news.Id = Guid.NewGuid();
                    var utility = Resolve<Utility>();
                    news.Date = utility.GetCurrentDateTimeInTimeZone();
                    news.ImageUrl = string.Empty;
                    var newsDb = await _newsRepository.GetByExpression(n => n.Header.ToLower().Equals(news.Header.ToLower()));
                    var accountRepository = Resolve<IAccountRepository>();
                    var accountId = await accountRepository.GetByExpression(n => n.Id == news.AccountId);
                    if (newsDb != null)
                    {
                        result = BuildAppActionResultError(result, $"The news whose header: {newsDb.Header} has existed!");
                    }
                    result.Result.Data = await _newsRepository.Insert(news);
                    await _unitOfWork.SaveChangeAsync();

                    if (!BuildAppActionResultIsError(result))
                    {
                        var fileService = Resolve<IFileService>();
                        string url = $"{SD.FirebasePathName.NEWS_PREFIX}{news.Id}";
                        var resultFirebase = await fileService.UploadFileToFirebase(NewsRequest.ImgUrl, url);
                        if (resultFirebase != null && resultFirebase.IsSuccess)
                        {
                            news.ImageUrl = Convert.ToString(resultFirebase.Result.Data);
                            await _unitOfWork.SaveChangeAsync();
                        }

                        if (!BuildAppActionResultIsError(result))
                        {
                            scope.Complete();
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = BuildAppActionResultError(result, ex.Message);
                    _logger.LogError(ex.Message, this);
                }
                return result;
            }
        }

        public async Task<AppActionResult> DeleteNewsById(Guid id)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                AppActionResult result = new AppActionResult();
                try
                {
                    var newsDb = _newsRepository.GetById(id);
                    if (newsDb == null)
                    {
                        result = BuildAppActionResultError(result, $"The news with {id} not found!");
                    }
                    else
                    {
                        var fileService = Resolve<IFileService>();
                        string url = $"{SD.FirebasePathName.NEWS_PREFIX}{newsDb.Id}";

                        var resultFirebase = await fileService.DeleteFileFromFirebase(url);

                        if (resultFirebase != null && resultFirebase.IsSuccess)
                        {
                            result.Result.Data = await _newsRepository.DeleteById(id);
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
                    result = BuildAppActionResultError(result, ex.Message);
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
                    var newsList = await _newsRepository.GetAllDataByExpression(null, a => a.Account);
                    var fileService = Resolve<IFileService>();
                    var SD = Resolve<SD>();

                    //    var news = Utility.ConvertIOrderQueryAbleToList(newsList);

                    var newss = Utility.ConvertListToIOrderedQueryable(newsList);

                    if (newsList.Any())
                    {
                        if (pageIndex <= 0) pageIndex = 1;
                        if (pageSize <= 0) pageSize = SD.MAX_RECORD_PER_PAGE;
                        int totalPage = DataPresentationHelper.CalculateTotalPageSize(newsList.Count(), pageSize);

                        if (sortInfos != null)
                        {
                            newsList = DataPresentationHelper.ApplySorting(newsList, sortInfos);
                        }
                        if (pageIndex > 0 && pageSize > 0)
                        {
                            newsList = DataPresentationHelper.ApplyPaging(newsList, pageIndex, pageSize);
                        }
                        result.Result.Data = newsList;
                        result.Result.TotalPage = totalPage;
                    }
                    else
                    {
                        result.Messages.Add("EMpty news list");
                    }

                    if (!BuildAppActionResultIsError(result))
                    {
                        scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    result = BuildAppActionResultError(result, ex.Message);
                    _logger.LogError(ex.Message, this);
                }
                return result;
            }
        }

        public async Task<AppActionResult> GetNewsById(Guid id)
        {
            AppActionResult result = new AppActionResult();
            try
            {
                var newsDb = await _newsRepository.GetById(id);
                if (newsDb != null)
                {
                    result.Result.Data = newsDb;
                }
            }
            catch (Exception ex)
            {
                result = BuildAppActionResultError(result, ex.Message);
                _logger.LogError(ex.Message, this);
            }
            return result;
        }

        public async Task<AppActionResult> UpdateNews(NewsRequest NewsRequest)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                AppActionResult result = new AppActionResult();
                try
                {
                    var newsDb = await _newsRepository.GetByExpression(n => n.Id.Equals(NewsRequest.Id));
                    if (newsDb == null)
                    {
                        result = BuildAppActionResultError(result, $"The news with {NewsRequest.Id} not found !");
                    }
                    else
                    {
                        var fileService = Resolve<IFileService>();
                        string url = $"{SD.FirebasePathName.NEWS_PREFIX}{newsDb.Id}";
                        var resultFirebase = await fileService.DeleteFileFromFirebase(url);
                        if (resultFirebase != null && resultFirebase.IsSuccess)
                        {
                            var uploadFileResult = await fileService.UploadFileToFirebase(NewsRequest.ImgUrl, url);
                            if (uploadFileResult.IsSuccess)
                            {
                                var news = _mapper.Map<News>(NewsRequest);
                                newsDb.ImageUrl = Convert.ToString(uploadFileResult.Result.Data);
                                newsDb.Content = news.Content;
                                newsDb.Header = news.Header;
                                result.Result.Data = newsDb;
                                await _unitOfWork.SaveChangeAsync();
                            }
                        }
                    }

                    if (!BuildAppActionResultIsError(result))
                    {
                        scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    result = BuildAppActionResultError(result, ex.Message);
                    _logger.LogError(ex.Message, this);
                }
                return result;
            }
        }
    }
}