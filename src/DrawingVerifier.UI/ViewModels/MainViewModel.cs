namespace DrawingVerifier.UI.ViewModels;

using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrawingVerifier.Application.DTOs;
using DrawingVerifier.Application.Interfaces;
using DrawingVerifier.Application.UseCases;
using DrawingVerifier.Domain.Entities;
using DrawingVerifier.Domain.Interfaces;
using Microsoft.Win32;

public partial class MainViewModel : ObservableObject
{
    private const string ReportFileName = "VerificationReport.pdf";

    private readonly ISkillRepository _skillRepository;
    private readonly AnalyzeDrawingUseCase _analyzeDrawingUseCase;
    private readonly IReportGenerator _reportGenerator;

    [ObservableProperty]
    private ObservableCollection<VerificationSkill> _skills = [];

    [ObservableProperty]
    private VerificationSkill? _selectedSkill;

    [ObservableProperty]
    private ObservableCollection<string> _pdfFiles = [];

    [ObservableProperty]
    private ObservableCollection<VerificationReport> _reports = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready.";

    public MainViewModel(
        ISkillRepository skillRepository,
        AnalyzeDrawingUseCase analyzeDrawingUseCase,
        IReportGenerator reportGenerator)
    {
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _analyzeDrawingUseCase = analyzeDrawingUseCase ?? throw new ArgumentNullException(nameof(analyzeDrawingUseCase));
        _reportGenerator = reportGenerator ?? throw new ArgumentNullException(nameof(reportGenerator));
    }

    [RelayCommand]
    private async Task LoadSkillsAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Loading verification skills...";

            var loadedSkills = await _skillRepository.GetAllAsync();

            Skills.Clear();
            foreach (var skill in loadedSkills)
            {
                Skills.Add(skill);
            }

            if (SelectedSkill is null && Skills.Count > 0)
            {
                SelectedSkill = Skills[0];
            }

            StatusMessage = $"Loaded {Skills.Count} skill(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading skills: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddPdfFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select PDF Drawings",
            Filter = "PDF Files (*.pdf)|*.pdf",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            var addedCount = 0;
            foreach (var fileName in dialog.FileNames)
            {
                if (!PdfFiles.Contains(fileName))
                {
                    PdfFiles.Add(fileName);
                    addedCount++;
                }
            }

            StatusMessage = $"Added {addedCount} PDF file(s). Total: {PdfFiles.Count}.";
        }
    }

    [RelayCommand]
    private async Task RunVerificationAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (SelectedSkill is null)
        {
            StatusMessage = "Please select a verification skill before running.";
            return;
        }

        if (PdfFiles.Count == 0)
        {
            StatusMessage = "Please add at least one PDF file to verify.";
            return;
        }

        try
        {
            IsBusy = true;
            Reports.Clear();

            var filesToProcess = PdfFiles.ToList();
            for (var i = 0; i < filesToProcess.Count; i++)
            {
                var filePath = filesToProcess[i];
                var fileName = Path.GetFileName(filePath);

                StatusMessage = $"Verifying ({i + 1}/{filesToProcess.Count}): {fileName}...";

                if (!File.Exists(filePath))
                {
                    Reports.Add(new VerificationReport(
                        fileName,
                        SelectedSkill.Name,
                        isValid: false,
                        errors: [$"File not found: {filePath}"]));
                    continue;
                }

                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var pdfBase64 = Convert.ToBase64String(fileBytes);

                var report = await _analyzeDrawingUseCase.ExecuteAsync(fileName, pdfBase64, SelectedSkill);
                Reports.Add(report);
            }

            var validCount = Reports.Count(r => r.IsValid);
            var summary = $"Verification completed. {validCount}/{Reports.Count} passed.";

            await GenerateReportAsync(summary);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Verification error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task GenerateReportAsync(string summary)
    {
        var outputPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            ReportFileName);

        StatusMessage = $"{summary} Generating PDF report...";

        try
        {
            // Snapshot the collection and render off the UI thread to keep the window responsive.
            var reportsSnapshot = Reports.ToList();
            await Task.Run(() => _reportGenerator.GeneratePdfReport(reportsSnapshot, outputPath));

            StatusMessage = $"{summary} Report saved to Desktop: {outputPath}";
        }
        catch (Exception ex)
        {
            // Report failures (e.g., the file is locked by a PDF viewer) must not hide the verification results.
            StatusMessage = $"{summary} Failed to save PDF report: {ex.Message}";
        }
    }
}
