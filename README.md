# Dungeon Overlords ⚔️ | El JUEGO | LEER SECCION DE COMO COMPILAR

![Unity](https://img.shields.io/badge/Unity-6000.0.4.3f1-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-%23178600.svg?style=for-the-badge&logo=csharp&logoColor=white)
![Status](https://img.shields.io/badge/Status-En_Desarrollo-orange?style=for-the-badge)

**Dungeon Overlords** es un videojuego de acción rápida estilo *Dungeon Crawler* en **2D**, desarrollado como proyecto grupal para el curso de Programación de Videojuegos 2026 en la **Universidad Nacional Mayor de San Marcos (UNMSM)**.

## 📝 Descripción
El juego propone una experiencia *Top-Down* (vista aérea) que recrea las mecánicas clásicas del género arcade: combate frenético, exploración y superación de retos en mazmorras cerradas. El objetivo principal es sobrevivir a un bucle infinito donde la dificultad escala automáticamente al superar cada nivel.

## ⚙️ Características Técnicas (Planificadas)
El núcleo del proyecto se estructurará bajo los siguientes pilares de desarrollo:

* **Generación Aleatoria de Mazmorras:** Creación de mapas basados en un diseño en Grid (matriz bidimensional $(X,Y)$) con caminos interconectados de forma dinámica.
* **Bucle de Dificultad Progresiva:** Escalado lineal y automático de los parámetros de vida y daño de los enemigos conforme se avanza de nivel.
* **Perspectiva 2D Top-Down:** Control del jugador enfocado en físicas de ataque, movimiento y combate fluido en entornos cerrados.

## 📁 Estructura del Proyecto

*Por definir a medida que se estructure el repositorio base en Git.*

## 👥 Autores

* **Enrique Julca Delgado** - *Desarrollo* - [ENtiemEN](https://github.com/ENtiemEN)  
* **Mark Christian Quispe Gonzales** - *Lider de Desarrollo Integral* - [Mark0poll0](https://github.com/Mark0poll0)
* **Institución:** Universidad Nacional Mayor de San Marcos (UNMSM)
* **Carrera:** Computación Científica - Curso de Videojuegos



# 🛠️ Instalación y Configuración

Antes de comenzar a desarrollar o compilar el proyecto, asegúrate de contar con las siguientes herramientas instaladas y configuradas correctamente en **Windows**.

---

### 📥 1. Clonar el repositorio

Abre una terminal de **Git Bash** :

1. **Clonar el repositorio:** Clona este repositorio o descarga el código fuente:
   ```bash
   git clone https://github.com/Mark0poll0/Proyecto_videojuegos_2026_GRUPAL.git
   ```
2. **Importar en Unity Hub:** Abre **Unity Hub**, haz clic en **Add** (Añadir proyecto desde disco) y selecciona la carpeta raíz del proyecto.
3. **Abrir el Proyecto:** Asegúrate de seleccionar la versión recomendada de Unity y abre el proyecto.
4. **Abrir la Escena (Importante):** Al abrir el proyecto por primera vez, es normal que Unity se muestre vacío o con una escena por defecto llamada *Untitled*. Para cargar el juego:
   * En la pestaña *Project* (explorador de carpetas inferior), navega a la siguiente ruta:
     `Assets` $\rightarrow$ `PROYECTO` $\rightarrow$ `SCENES`
   * Haz **doble clic** sobre la escena **`MainMenu`** (icono de Unity) para cargarla.
5. **Compilar y Jugar:** Una vez cargada la escena en la jerarquía, haz clic en el botón de **Play** (Reproducir/Play en la parte superior central) para compilar y ejecutar la partida.
---
### 🎮 2. Instalar Unity

Este proyecto fue desarrollado utilizando la siguiente versión de Unity:

> **Unity 6 — Versión 6000.0.43f1**

⚠️ Es importante utilizar **exactamente la misma versión** para evitar problemas de compatibilidad, errores de compilación o conflictos con escenas y paquetes.

Instala esta versión mediante **Unity Hub**.

---
### 💻 Visual Studio

Instala **Visual Studio 2022** y selecciona las siguientes cargas de trabajo desde el **Visual Studio Installer**:

| Componente | Requerido |
|------------|------------|
| Desarrollo de juegos con Unity | ✅ |
| Desarrollo de juegos con C++ | ✅ |
| Desarrollo para escritorio con C++ | ✅ |

> 📌 Estas dependencias son necesarias para garantizar la correcta compilación y depuración del proyecto.
---
## 📁 Estructura del Proyecto
Los elementos más importantes del proyecto se organizan bajo la carpeta principal `Assets/PROYECTO`:
* 📂 **`SCENES`**: Contiene las escenas del juego:
  * `MainMenu.unity` - Menú de inicio con selección de opciones de diseño.
  * `mapa 1.unity` - Escena principal del gameplay y generación de mazmorras.
* 📂 **`CODE/Script`**: Scripts de control en C# organizados por funciones:
  * `Player_Controller.cs` y `PlayerBuffs.cs` - Control del personaje y estadísticas.
  * `ProceduralMapGenerator.cs` - Algoritmo de generación de niveles en cuadrícula.
  * `DifficultyDirector.cs` - Administrador de oleadas y escalado.
  * `EnemyController.cs` y `EnemyHealth.cs` - Lógica de inteligencia artificial y daño de enemigos.
  * `StatsPanelUIManager.cs`, `HudStatsUIManager.cs`, `TurboUIManager.cs` - Controladores del HUD e interfaces del juego.
  * `AudioManager.cs` y `JuiceManager.cs` - Lógica de sonido y efectos visuales de impacto.
* 📂 **`PREFABS`**: Prefabs configurados para enemigos (Green, Blue, Red), proyectiles, plantas y elementos del mapa.
* 📂 **`ART`**: Spritesheets de animaciones del jugador, enemigos y tilesets del entorno.
* 📂 **`AUDIO`**: Clips de sonido para ataques, impactos, alertas y música.
---
