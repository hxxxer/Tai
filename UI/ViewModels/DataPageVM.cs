using Core.Librarys;
using Core.Librarys.Image;
using Core.Models;
using Core.Models.Db;
using Core.Servicers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UI.Controls;
using UI.Controls.Charts.Model;
using UI.Controls.Select;
using UI.Models;
using UI.Servicers;
using UI.Views;
using static UI.Controls.SettingPanel.SettingPanel;

namespace UI.ViewModels
{
    public class DataPageVM : DataPageModel
    {
        public Command ToDetailCommand { get; set; }
        private readonly IData data;
        private readonly MainViewModel main;
        private readonly IAppContextMenuServicer appContextMenuServicer;
        private readonly IAppConfig appConfig;
        private readonly IWebData _webData;
        private readonly IWebSiteContextMenuServicer _webSiteContextMenu;
        private readonly ICategorys _categorys;
        private const int CategoryFilterAll = -1;
        private const int CategoryFilterUncategorized = 0;

        public DataPageVM(IData data, MainViewModel main, IAppContextMenuServicer appContextMenuServicer, IAppConfig appConfig, IWebData webData, IWebSiteContextMenuServicer webSiteContextMenu, ICategorys categorys)
        {
            this.data = data;
            this.main = main;
            this.appContextMenuServicer = appContextMenuServicer;
            this.appConfig = appConfig;
            _webData = webData;
            _webSiteContextMenu = webSiteContextMenu;
            _categorys = categorys;

            ToDetailCommand = new Command(new Action<object>(OnTodetailCommand));

            Init();
        }

        public override void Dispose()
        {
            PropertyChanged -= DataPageVM_PropertyChanged;

            base.Dispose();
        }
        private void Init()
        {
            //LoadData(DateTime.Now.Date);
            PropertyChanged += DataPageVM_PropertyChanged;

            TabbarData = new System.Collections.ObjectModel.ObservableCollection<string>()
            {
                "按天","按月","按年"
            };

            TabbarSelectedIndex = 0;

            AppContextMenu = appContextMenuServicer.GetContextMenu();
            LoadCategoryOptions();
        }

        private void OnTodetailCommand(object obj)
        {
            var data = obj as ChartsDataModel;

            if (data != null)
            {
                var model = data.Data as DailyLogModel;
                if (model != null && model.AppModel != null)
                {
                    main.Data = model.AppModel;
                    main.Uri = nameof(DetailPage);
                }
                else
                {
                    main.Data = data.Data as Core.Models.Db.WebSiteModel;
                    main.Uri = nameof(WebSiteDetailPage);
                }
            }
        }

        private void DataPageVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

            if (e.PropertyName == nameof(DayDate))
            {
                LoadData(DayDate);
            }
            else if (e.PropertyName == nameof(MonthDate))
            {
                LoadData(MonthDate);
            }
            else if (e.PropertyName == nameof(YearDate))
            {
                LoadData(YearDate);
            }
            else if (e.PropertyName == nameof(TabbarSelectedIndex))
            {
                if (TabbarSelectedIndex == 0)
                {
                    if (DayDate == DateTime.MinValue)
                    {
                        DayDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                    }

                }
                else if (TabbarSelectedIndex == 1)
                {
                    if (MonthDate == DateTime.MinValue)
                    {
                        MonthDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    }
                }
                else if (TabbarSelectedIndex == 2)
                {
                    if (YearDate == DateTime.MinValue)
                    {
                        YearDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    }
                }
            }
            else if (e.PropertyName == nameof(ShowType))
            {
                if (ShowType.Id == 0)
                {
                    AppContextMenu = appContextMenuServicer.GetContextMenu();
                }
                else
                {
                    AppContextMenu = _webSiteContextMenu.GetContextMenu();
                }

                // 展示类型切换时重建分类筛选，并回到“全部”
                LoadCategoryOptions();
                LoadData(DayDate, 0);
                LoadData(MonthDate, 1);
                LoadData(YearDate, 2);
            }
            else if (e.PropertyName == nameof(SelectedCategory))
            {
                // 分类筛选变化时，仅刷新当前分页对应的数据
                ReloadCurrentTabData();
            }
        }



        /// <summary>
        /// 读取分类筛选项。
        /// - 应用模式：读取应用分类
        /// - 网站模式：读取网站分类
        /// </summary>
        private void LoadCategoryOptions()
        {
            var options = new List<SelectItemModel>
            {
                new SelectItemModel()
                {
                    Id = CategoryFilterAll,
                    Name = "全部"
                },
                new SelectItemModel()
                {
                    Id = CategoryFilterUncategorized,
                    Name = "未分类"
                }
            };

            if (ShowType.Id == 0)
            {
                // 应用分类
                foreach (var item in _categorys.GetCategories())
                {
                    options.Add(new SelectItemModel()
                    {
                        Id = item.ID,
                        Name = item.Name,
                        Img = item.IconFile,
                        Data = item
                    });
                }
            }
            else
            {
                // 网站分类
                foreach (var item in _webData.GetWebSiteCategories())
                {
                    options.Add(new SelectItemModel()
                    {
                        Id = item.ID,
                        Name = item.Name,
                        Img = item.IconFile,
                        Data = item
                    });
                }
            }

            CategoryOptions = options;
            SelectedCategory = CategoryOptions.FirstOrDefault();
        }

        private void ReloadCurrentTabData()
        {
            if (TabbarSelectedIndex == 0)
            {
                LoadData(DayDate);
            }
            else if (TabbarSelectedIndex == 1)
            {
                LoadData(MonthDate);
            }
            else
            {
                LoadData(YearDate);
            }
        }

        #region 读取数据




        private async void LoadData(DateTime date, int dataType_ = -1)
        {
            await Task.Run(() =>
            {
                DateTime dateStart = date, dateEnd = date;

                dataType_ = dataType_ == -1 ? TabbarSelectedIndex : dataType_;

                if (dataType_ == 1)
                {
                    dateStart = new DateTime(date.Year, date.Month, 1);
                    dateEnd = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
                }
                else if (dataType_ == 0)
                {
                    dateStart = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
                    dateEnd = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);
                }
                else if (dataType_ == 2)
                {
                    dateStart = new DateTime(date.Year, 1, 1, 0, 0, 0);
                    dateEnd = new DateTime(date.Year, 12, DateTime.DaysInMonth(date.Year, 12), 23, 59, 59);
                }

                List<ChartsDataModel> chartData = new List<ChartsDataModel>();
                if (ShowType.Id == 0)
                {
                    var result = data.GetDateRangelogList(dateStart, dateEnd);
                    result = FilterAppLogsByCategory(result);
                    chartData = MapToChartsData(result);
                }
                else
                {
                    var result = _webData.GetWebSiteLogList(dateStart, dateEnd);
                    result = FilterWebSitesByCategory(result);
                    chartData = MapToChartsWebData(result);
                }


                if (dataType_ == 0)
                {
                    Data = chartData;
                }
                else if (dataType_ == 1)
                {
                    MonthData = chartData;
                }
                else
                {
                    YearData = chartData;
                }

            });
        }

        /// <summary>
        /// 根据当前分类筛选过滤应用统计列表。
        /// </summary>
        private IEnumerable<DailyLogModel> FilterAppLogsByCategory(IEnumerable<DailyLogModel> source_)
        {
            if (SelectedCategory == null || SelectedCategory.Id == CategoryFilterAll)
            {
                return source_;
            }

            if (SelectedCategory.Id == CategoryFilterUncategorized)
            {
                return source_.Where(m => m.AppModel == null || m.AppModel.CategoryID == 0 || m.AppModel.Category == null);
            }

            return source_.Where(m => m.AppModel != null && m.AppModel.CategoryID == SelectedCategory.Id);
        }

        /// <summary>
        /// 根据当前分类筛选过滤网站统计列表。
        /// </summary>
        private IEnumerable<WebSiteModel> FilterWebSitesByCategory(IEnumerable<WebSiteModel> source_)
        {
            if (SelectedCategory == null || SelectedCategory.Id == CategoryFilterAll)
            {
                return source_;
            }

            if (SelectedCategory.Id == CategoryFilterUncategorized)
            {
                return source_.Where(m => m.CategoryID == 0 || m.Category == null);
            }

            return source_.Where(m => m.CategoryID == SelectedCategory.Id);
        }

        #region 处理数据
        private List<ChartsDataModel> MapToChartsData(IEnumerable<Core.Models.DailyLogModel> list)
        {
            var resData = new List<ChartsDataModel>();
            try
            {
                var config = appConfig.GetConfig();

                foreach (var item in list)
                {
                    var bindModel = new ChartsDataModel();
                    bindModel.Data = item;
                    bindModel.Name = !string.IsNullOrEmpty(item.AppModel?.Alias) ? item.AppModel.Alias : string.IsNullOrEmpty(item.AppModel?.Description) ? item.AppModel.Name : item.AppModel.Description;
                    bindModel.Value = item.Time;
                    bindModel.Tag = Time.ToString(item.Time);
                    bindModel.PopupText = item.AppModel?.File;
                    bindModel.Icon = item.AppModel?.IconFile;
                    bindModel.BadgeList = new List<ChartBadgeModel>();
                    if (item.AppModel.Category != null)
                    {
                        bindModel.BadgeList.Add(new ChartBadgeModel()
                        {
                            Name = item.AppModel.Category.Name,
                            Color = item.AppModel.Category.Color,
                            Type = ChartBadgeType.Category
                        });
                    }
                    if (config.Behavior.IgnoreProcessList.Contains(item.AppModel.Name))
                    {
                        bindModel.BadgeList.Add(ChartBadgeModel.IgnoreBadge);
                    }
                    resData.Add(bindModel);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }

            return resData;
        }

        private List<ChartsDataModel> MapToChartsWebData(IEnumerable<Core.Models.Db.WebSiteModel> list)
        {
            var resData = new List<ChartsDataModel>();
            try
            {
                var config = appConfig.GetConfig();

                foreach (var item in list)
                {
                    var bindModel = new ChartsDataModel();
                    bindModel.Data = item;
                    bindModel.Name = !string.IsNullOrEmpty(item.Alias) ? item.Alias : item.Title;
                    bindModel.Value = item.Duration;
                    bindModel.Tag = Time.ToString(item.Duration);
                    bindModel.PopupText = item.Domain;
                    bindModel.Icon = item.IconFile;
                    bindModel.BadgeList = new List<ChartBadgeModel>();
                    if (item.Category != null)
                    {
                        bindModel.BadgeList.Add(new ChartBadgeModel()
                        {
                            Name = item.Category.Name,
                            Color = item.Category.Color,
                            Type = ChartBadgeType.Category
                        });
                    }
                    if (config.Behavior.IgnoreURLList.Contains(item.Domain))
                    {
                        bindModel.BadgeList.Add(ChartBadgeModel.IgnoreBadge);
                    }
                    resData.Add(bindModel);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
            return resData;
        }
        #endregion
        #endregion
    }
}
