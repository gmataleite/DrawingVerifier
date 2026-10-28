# DrawingVerifier - Especificação da Arquitetura

## 1. Visão Geral
Aplicação desktop em WPF (C# / .NET 9) para automatizar a verificação de desenhos mecânicos em PDF. O sistema extrai dados do PDF via API do Gemini 1.5 Pro (estruturados em JSON) e cruza os dados localmente usando regras matemáticas dinâmicas.

## 2. Stack Tecnológico
- **Frontend:** WPF com padrão MVVM (usando CommunityToolkit.Mvvm).
- **Core/Domain:** C# 13, .NET 9.
- **Banco de Dados Local:** LiteDB (arquitetura NoSQL em arquivo local).
- **Motor de Regras (Avaliação Dinâmica):** NCalc.
- **Geração de Relatórios:** QuestPDF.
- **Injeção de Dependência:** Microsoft.Extensions.DependencyInjection.

## 3. Arquitetura (Clean Architecture)
A solução deve ser dividida rigorosamente em:
- `DrawingVerifier.Domain`: Entidades (Skill, ValidationRule, ExtractedDrawing), Interfaces de Repositório e Enums. Sem dependências externas.
- `DrawingVerifier.Application`: Casos de uso, interfaces de serviços externos (IApiService), lógica de avaliação com NCalc.
- `DrawingVerifier.Infrastructure`: Implementação do LiteDB, chamadas HTTP HttpClient para a API do Gemini e gerador do QuestPDF.
- `DrawingVerifier.UI`: Projeto WPF, Views, ViewModels e configuração do App.xaml.cs (DI Container).

## 4. O Fluxo de Negócio (Obrigatório)
1. O usuário seleciona uma "Skill" (Perfil do Cliente, ex: Relco).
2. O usuário carrega `N` arquivos PDF.
3. O sistema envia cada PDF para o Gemini, injetando o Schema JSON atrelado à Skill.
4. O sistema recebe os JSONs, carrega no Domain e itera sobre as regras de validação (ex: sobremetal).
5. O sistema compila os erros e gera um relatório PDF final.