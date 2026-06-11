# 🚀 ProyExplorador

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![WPF](https://img.shields.io/badge/WPF-Windows-purple)
![C%23](https://img.shields.io/badge/C%23-Language-green)
![License](https://img.shields.io/badge/License-Academic-orange)

## 📖 Descripción

ProyExplorador es una aplicación de escritorio desarrollada en **C#**, **WPF** y **.NET 8** que integra múltiples herramientas para la exploración, visualización, análisis y gestión de archivos.

La aplicación permite navegar por directorios, visualizar diferentes formatos de datos, realizar búsquedas avanzadas, gestionar archivos multimedia, monitorear el rendimiento del sistema, limpiar archivos temporales y trabajar con bases de datos SQL desde una interfaz moderna basada en Material Design.

---

## ✨ Características Principales

### 📁 Explorador de Archivos
- Navegación por directorios y unidades de almacenamiento.
- Visualización de carpetas y archivos.
- Gestión básica de archivos.
- Miniaturas para imágenes y contenido multimedia.

### 📖 Lector de Archivos
- Apertura y lectura de documentos.
- Visualización organizada del contenido.
- Manejo de múltiples formatos.

### 🔍 Sistema de Búsqueda
- Búsqueda rápida de archivos.
- Filtrado dinámico de resultados.
- Navegación eficiente.

### 📊 Visualizador de Datos
Compatible con:

- CSV
- JSON
- XML
- HTML

Funciones:
- Lectura de datos estructurados.
- Visualización tabular.
- Procesamiento y análisis de información.

### 🔄 Conversión de Archivos
- Conversión entre diferentes formatos de datos.
- Procesamiento automatizado.
- Exportación de información.

### 📈 Estadísticas y Monitoreo
- Monitoreo del rendimiento del sistema.
- Visualización de métricas.
- Gráficas interactivas mediante LiveCharts.

### 🎵 Gestión Multimedia
- Exploración de archivos multimedia.
- Lectura de metadatos.
- Gestión de contenido audiovisual.

### 📷 Cámara Web
- Captura desde dispositivos compatibles.
- Integración mediante AForge.NET.

### 🧹 Limpieza del Sistema
- Eliminación de archivos temporales.
- Optimización de almacenamiento.
- Liberación de espacio en disco.

### 🗄️ Bases de Datos
- Conexión a SQL Server.
- Soporte para MySQL.
- Migración de datos.
- Consulta y visualización de registros.

### ⚙️ Configuración Personalizada
- Administración de preferencias.
- Configuración de la aplicación.
- Persistencia de ajustes.

---

## 🏗️ Arquitectura del Proyecto

```text
ProyExplorador
│
├── Helpers
│   ├── AcrylicHelper
│   ├── AnimationHelper
│   └── Converters
│
├── Models
│   ├── AppSettings
│   ├── DriveItem
│   ├── FileItem
│   ├── NavigationItem
│   └── RecentFile
│
├── Parsers
│   ├── CsvParser
│   ├── JsonParser
│   ├── XmlParser
│   └── HtmlParser
│
├── Services
│   ├── FileService
│   ├── NavigationService
│   ├── ThumbnailService
│   ├── FileConverterService
│   ├── CameraService
│   ├── CleanupService
│   ├── SqlDataService
│   ├── SqlServerMigrationService
│   ├── MySqlMigrationService
│   └── PerformanceMonitor
│
├── Themes
│   ├── DarkTheme
│   └── Styles
│
├── ViewModels
│   ├── DashboardViewModel
│   ├── FileExplorerViewModel
│   ├── FileReaderViewModel
│   ├── DataViewerViewModel
│   ├── SearchViewModel
│   ├── MultimediaViewModel
│   ├── StatsViewModel
│   ├── CleanupViewModel
│   ├── SettingsViewModel
│   └── MainViewModel
│
└── MainWindow.xaml
```

---

## 🛠️ Tecnologías Utilizadas

- C#
- .NET 8
- WPF
- MVVM
- Dependency Injection
- Material Design

---

## 📚 Paquetes NuGet

| Paquete | Uso |
|----------|----------|
| CommunityToolkit.Mvvm | Implementación MVVM |
| MaterialDesignThemes | Diseño moderno |
| CsvHelper | Lectura de archivos CSV |
| LiveChartsCore | Gráficas y estadísticas |
| Microsoft.Data.SqlClient | SQL Server |
| ServiceStack.OrmLite.MySqlConnector | MySQL |
| AForge.Video | Cámara web |
| AForge.Video.DirectShow | Captura de video |
| DocumentFormat.OpenXml | Documentos Office |
| QuestPDF | Generación de PDF |
| iTextSharp | Manipulación PDF |
| TagLibSharp | Metadatos multimedia |

---

## 💻 Requisitos

### Sistema Operativo
- Windows 10 o superior

### Software
- Visual Studio 2022
- .NET 8 SDK

---

## ⚡ Instalación

### 1. Clonar el repositorio

```bash
git clone https://github.com/TU-USUARIO/ProyExplorador.git
```

### 2. Entrar al proyecto

```bash
cd ProyExplorador
```

### 3. Restaurar dependencias

```bash
dotnet restore
```

### 4. Compilar

```bash
dotnet build
```

### 5. Ejecutar

```bash
dotnet run
```

---

## 📸 Capturas de Pantalla

### Dashboard

Agrega aquí una captura:

```text
docs/dashboard.png
```

### Explorador de Archivos

```text
docs/explorer.png
```

### Estadísticas

```text
docs/stats.png
```

### Multimedia

```text
docs/multimedia.png
```

---

## 🔧 Funcionalidades Destacadas

✅ Exploración de archivos

✅ Visualización de datos CSV, JSON, XML y HTML

✅ Conversión de formatos

✅ Estadísticas del sistema

✅ Gestión multimedia

✅ Captura desde cámara web

✅ Limpieza de archivos temporales

✅ SQL Server y MySQL

✅ Interfaz moderna Material Design

---

## 👨‍💻 Autores

**Leonardo Martínez Salinas**
**Kevin Anselmo Orozco Aguayo**
Proyecto académico desarrollado con tecnologías .NET para la gestión y exploración avanzada de archivos.

---

## 📄 Licencia

Este proyecto se distribuye con fines académicos y educativos.
