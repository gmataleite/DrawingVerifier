# DrawingVerifier - Architecture Specification

## 1. Overview
WPF desktop application (C# / .NET 9) to automate the verification of mechanical drawings in PDF format. The system extracts data from the PDF via the Gemini 1.5 Pro API (structured as JSON) and cross-references the data locally using dynamic mathematical rules.

## 2. Tech Stack
- **Frontend:** WPF with MVVM pattern (using CommunityToolkit.Mvvm).
- **Core/Domain:** C# 13, .NET 9.
- **Local Database:** LiteDB (NoSQL embedded database).
- **Rules Engine (Dynamic Evaluation):** NCalc.
- **Report Generation:** QuestPDF.
- **Dependency Injection:** Microsoft.Extensions.DependencyInjection.

## 3. Architecture (Clean Architecture)
The solution must be strictly divided into:
- `DrawingVerifier.Domain`: Entities (Skill, ValidationRule, ExtractedDrawing), Repository Interfaces, and Enums. No external dependencies.
- `DrawingVerifier.Application`: Use cases, external service interfaces (IApiService), NCalc evaluation logic.
- `DrawingVerifier.Infrastructure`: LiteDB implementation, HttpClient calls for the Gemini API, and QuestPDF generator.
- `DrawingVerifier.UI`: WPF project, Views, ViewModels, and App.xaml.cs configuration (DI Container).

## 4. Business Flow
1. The user selects a "Skill" (Client Profile, e.g., Relco).
2. The user loads `N` PDF files.
3. The system sends each PDF to Gemini, injecting the JSON Schema tied to the selected Skill.
4. The system receives the JSON responses, loads them into the Domain, and iterates over the validation rules (e.g., overmetal rules).
5. The system compiles the errors and generates a final PDF report.