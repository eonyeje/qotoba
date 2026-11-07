# Qotoba Workbench for Rider

The Workbench pane inside Rider: the same page the browser shows, in a tool window on the right, with the token passed once, the Rider layout selected, and "Open in Rider" opening the file in this IDE. The title shows how many cards wait on you.

## Build

Built against the Rider installed on this machine (`riderHome` in `gradle.properties`), with Rider's own Java:

```powershell
$env:JAVA_HOME = "$env:LOCALAPPDATA\Programs\Rider\jbr"; .\gradlew.bat buildPlugin
```

The plugin lands in `build/distributions/qotoba-workbench-rider-<version>.zip`.

## Install

Settings › Plugins › ⚙ › Install Plugin from Disk, pick the zip, restart Rider. Or run `.\install.ps1`, which unpacks it into the current Rider's plugins folder; then restart Rider.

## Use

View › Tool Windows › Workbench, Tools › Qotoba Workbench, or Ctrl+Alt+W. Settings › Tools › Qotoba Workbench holds the service address and the token (by default read from `%USERPROFILE%\.qotoba-workbench\token`).
