# DrawingVerifier - User Manual & Operating Guide

A practical step-by-step guide for Quality Assurance (QA) engineers, draftspersons, and mechanical inspectors using **DrawingVerifier**.

---

## 1. Introduction

Mechanical fabrication drawings require rigorous checks before entering manufacturing. A single overlooked dimension error, incorrect plate thickness, missing overmetal (+100mm) for field fitting, or omitted weld symbol can lead to expensive scrap or shop delays.

**DrawingVerifier** automates this audit process by cross-referencing Bill of Materials (BOM) items, view dimensions, title blocks, and engineering notes using Google Gemini's advanced multimodal vision and mathematical validation rules.

---

## 2. Initial Setup (One-Time)

Before running your first verification:

1. Obtain a **Google Gemini API Key** from [Google AI Studio](https://aistudio.google.com/app/apikey).
2. Launch `DrawingVerifier.UI.exe`.
3. In the top-right toolbar, click **Settings**.
4. In the **Settings** window:
   - Paste your API key into the **Gemini API Key** field.
   - Choose your **Gemini Model** (default: `gemini-3.8-flash`; optional: `gemini-4-argon` for deep reasoning or `gemini-3.5-flash-lite` for fast batch audits).
   - Leave **Enable High-DPI Quadrant Tiling (300 DPI)** checked (recommended for standard A1/A0 mechanical drawings).
   - Click **Save**.

Your credentials are encrypted using Windows DPAPI and stored securely on your machine.

---

## 3. Main Interface Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Drawing Verifier                                                   [-][o][x]│
├─────────────────────────────────────────────────────────────────────────────┤
│ Verification Skill: [ Stainless Steel Ducts  v ] [Refresh] [Manage Skills]  │
│                                           [Add PDF Files...] [Settings]     │
├─────────────────────────────────────────────────────────────────────────────┤
│ Selected PDF Files                                                          │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ C:\Projects\Ductwork\DWG-001-REV-0.pdf                                  │ │
│ │ C:\Projects\Ductwork\DWG-002-REV-A.pdf                                  │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                        [Run Verification]   │
├─────────────────────────────────────────────────────────────────────────────┤
│ Verification Reports                                                        │
│ ┌───────────────────────────┬──────────────────┬───────┬────────┬─────────┐ │
│ │ File Name                 │ Skill            │ Valid │ Errors │ Warnings│ │
│ ├───────────────────────────┼──────────────────┼───────┼────────┼─────────┤ │
│ │ DWG-001-REV-0.pdf         │ Stainless Steel  │  [X]  │   0    │    0    │ │
│ │ DWG-002-REV-A.pdf         │ Stainless Steel  │  [ ]  │   2    │    1    │ │
│ └───────────────────────────┴──────────────────┴───────┴────────┴─────────┘ │
├─────────────────────────────────────────────────────────────────────────────┤
│ Status: Verification completed. 1/2 passed. Report saved to Desktop.        │
└─────────────────────────────────────────────────────────────────────────────┘
```

The main window is organized into 5 functional areas:
1. **Top Controls:** Select the client skill, refresh the skill list, open Skill Manager, load drawings, or open settings.
2. **Selected PDF Files:** Displays all drawings queued for analysis.
3. **Execution Button:** Starts batch verification.
4. **Verification Reports Table:** Summarizes file names, validation status (green check or empty), and counts of detected errors/warnings.
5. **Status Bar:** Real-time feedback on API communication, analysis progress, and report generation.

---

## 4. Running a Drawing Verification

### Step 1: Select Verification Profile (Skill)
Use the **Verification Skill** dropdown to select the target profile (e.g., `"Stainless Steel Ducts"`).

### Step 2: Queue Drawings
- Click **Add PDF Files...**
- In the file dialog, select one or multiple `.pdf` drawings.
- The queued files will appear in the **Selected PDF Files** box.

### Step 3: Run Verification
- Click **Run Verification**.
- DrawingVerifier will process each drawing sequentially:
  - High-resolution 300 DPI rasterization and quadrant decomposition.
  - Gemini AI multimodal extraction conforming to strict JSON schema.
  - Spatial proximity verification between balloons and dimensions.
  - Evaluation of all 10 standard mathematical and compliance rules.
- The progress bar in the status bar updates dynamically (`Verifying (1/5): DWG-001.pdf...`).

### Step 4: Review Results & PDF Report
- Review summary statuses directly in the **Verification Reports** DataGrid.
- A comprehensive **`VerificationReport.pdf`** is automatically generated and placed on your **Desktop**.

---

## 5. Understanding Verification Reports

### 5.1 On-Screen Indicators
- **Valid (Checked):** The drawing conforms 100% to all rules in the selected skill.
- **Valid (Unchecked):** One or more rules failed. Check the error list in the generated PDF report.
- **Errors Count:** Total number of strict rule violations (e.g., overmetal mismatch, missing title block field).
- **Warnings Count:** Advisories that require engineer attention but may not constitute complete non-conformance (e.g., spatial leader line distance $>600$, missing balloons for auxiliary items).

### 5.2 The PDF Audit Report (`VerificationReport.pdf`)
The generated PDF report contains:
- **Header:** Timestamp, total drawings analyzed, passed/failed breakdown.
- **File-by-File Breakdown:**
  - Status Badge: `PASSED` (Green) or `FAILED` (Red).
  - Associated skill name.
  - Bulleted list of errors with dynamic parameter values (e.g., `"[Balloon 4] BOM cut length does not match view dimension. (View: 540 + 100. BOM: 600mm)"`).
  - Bulleted list of spatial warnings and visual notes.

---

## 6. Customizing Skills & Rules

If your client or project requires custom checks (e.g., a 75mm overmetal standard or specific title block fields):

1. Click **Manage Skills** from the main window.
2. Select an existing skill or click **Add** to create a new profile.
3. In the **System Prompt** box, adjust Gemini's reading instructions.
4. In the **Validation Rules** table:
   - Click **Add Rule** to insert a new check.
   - Enter a **Rule Name** (e.g., `"Check Minimum Cut Length"`).
   - Enter an **NCalc Expression** (e.g., `ComprimentoBom >= 150`).
   - Enter a descriptive **Error Message** (e.g., `"[Balloon {BalaoRef}] Segment cut length ({ComprimentoBom}mm) is below the 150mm minimum threshold."`).
5. Click **Save Changes**.

---

## 7. Best Practices for Best Results

- **Vector vs. Scanned PDFs:** Native vector PDFs exported directly from AutoCAD, SolidWorks, Inventor, or Tekla provide superior extraction accuracy compared to low-resolution photocopied blueprints.
- **Sheet Orientation:** Ensure the PDF sheet is oriented right-side up. Gemini can read rotated text, but horizontal title blocks yield the fastest extraction.
- **Multi-Sheet Drawings:** Currently, DrawingVerifier audits the primary page (Page 0) of each drawing file. For multi-sheet packages, export individual sheets or separate PDFs.
- **Network Stability:** An active internet connection is required during the extraction phase to reach the Gemini API endpoint.
