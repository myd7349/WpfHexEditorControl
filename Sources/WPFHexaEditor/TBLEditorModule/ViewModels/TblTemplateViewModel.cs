//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WpfHexaEditor.TBLEditorModule.Models;
using WpfHexaEditor.TBLEditorModule.Services;

namespace WpfHexaEditor.TBLEditorModule.ViewModels
{
    /// <summary>
    /// ViewModel for TBL Template Dialog
    /// </summary>
    public class TblTemplateViewModel : INotifyPropertyChanged
    {
        private readonly TblTemplateService _templateService;
        private TblTemplate _selectedTemplate;
        private TemplateCategory _selectedCategory;
        private string _previewContent;

        public TblTemplateViewModel()
        {
            _templateService = new TblTemplateService();
            Categories = new ObservableCollection<TemplateCategory>();
            FilteredTemplates = new ObservableCollection<TblTemplate>();
            LoadTemplates();
        }

        public ObservableCollection<TemplateCategory> Categories { get; }
        public ObservableCollection<TblTemplate> FilteredTemplates { get; }

        public TblTemplate SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (_selectedTemplate != value)
                {
                    _selectedTemplate = value;
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public TemplateCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged();
                    UpdateFilteredTemplates();
                }
            }
        }

        public string PreviewContent
        {
            get => _previewContent;
            private set
            {
                if (_previewContent != value)
                {
                    _previewContent = value;
                    OnPropertyChanged();
                }
            }
        }

        private void LoadTemplates()
        {
            var allTemplates = _templateService.GetAllTemplates();
            var categoriesDict = new Dictionary<string, TemplateCategory>();

            foreach (var template in allTemplates)
            {
                if (!categoriesDict.ContainsKey(template.Category))
                {
                    categoriesDict[template.Category] = new TemplateCategory
                    {
                        Name = template.Category,
                        Templates = new ObservableCollection<TblTemplate>()
                    };
                }

                categoriesDict[template.Category].Templates.Add(template);
            }

            Categories.Clear();
            foreach (var category in categoriesDict.Values.OrderBy(c => c.Name))
            {
                Categories.Add(category);
            }

            // Select first category by default
            if (Categories.Count > 0)
            {
                SelectedCategory = Categories[0];
            }
        }

        public void OnCategorySelected(object selectedItem)
        {
            if (selectedItem is TemplateCategory category)
            {
                SelectedCategory = category;
            }
            else if (selectedItem is TblTemplate template)
            {
                SelectedTemplate = template;
            }
        }

        private void UpdateFilteredTemplates()
        {
            FilteredTemplates.Clear();

            if (SelectedCategory != null)
            {
                foreach (var template in SelectedCategory.Templates)
                {
                    FilteredTemplates.Add(template);
                }

                // Auto-select first template
                if (FilteredTemplates.Count > 0)
                {
                    SelectedTemplate = FilteredTemplates[0];
                }
            }
        }

        private void UpdatePreview()
        {
            if (SelectedTemplate != null)
            {
                try
                {
                    var tbl = SelectedTemplate.Load();
                    if (tbl != null)
                    {
                        // Generate preview (first 50 entries)
                        var entries = tbl.GetAllEntries().Take(50).ToList();
                        var preview = new System.Text.StringBuilder();

                        preview.AppendLine($"# Template: {SelectedTemplate.Name}");
                        preview.AppendLine($"# Category: {SelectedTemplate.Category}");
                        preview.AppendLine($"# Total Entries: {tbl.Length}");
                        preview.AppendLine();

                        foreach (var dte in entries)
                        {
                            preview.AppendLine($"{dte.Entry}={dte.Value}");
                        }

                        if (tbl.Length > 50)
                        {
                            preview.AppendLine();
                            preview.AppendLine($"... and {tbl.Length - 50} more entries");
                        }

                        PreviewContent = preview.ToString();
                    }
                }
                catch
                {
                    PreviewContent = "Error loading template preview";
                }
            }
            else
            {
                PreviewContent = string.Empty;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Template category for TreeView
    /// </summary>
    public class TemplateCategory
    {
        public string Name { get; set; }
        public ObservableCollection<TblTemplate> Templates { get; set; }
    }
}
