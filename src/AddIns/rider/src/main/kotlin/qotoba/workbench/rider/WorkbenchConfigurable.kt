package qotoba.workbench.rider

import com.intellij.openapi.options.BoundConfigurable
import com.intellij.openapi.ui.DialogPanel
import com.intellij.ui.dsl.builder.bindItem
import com.intellij.ui.dsl.builder.bindText
import com.intellij.ui.dsl.builder.columns
import com.intellij.ui.dsl.builder.panel

/** Settings › Tools › Qotoba Workbench. Three fields; nothing else is configurable from the IDE. */
class WorkbenchConfigurable : BoundConfigurable("Qotoba Workbench") {

    override fun createPanel(): DialogPanel {
        val s = WorkbenchSettings.instance().state
        return panel {
            row("Service address") {
                textField().bindText(s::serviceUrl).columns(40)
                    .comment("Where the Workbench service listens. On this machine: http://localhost:5290.")
            }
            row("Token") {
                textField().bindText(s::tokenOverride).columns(40)
                    .comment("Leave empty to read QOTOBA_WB_TOKEN or the token file the service writes in %USERPROFILE%\\.qotoba-workbench.")
            }
            row("Token file") {
                textField().bindText(s::tokenPath).columns(40)
                    .comment("Only if the token file is somewhere else.")
            }
            row("Layout") {
                comboBox(listOf("rider", "phone", "desktop")).bindItem({ s.layout }, { s.layout = it ?: "rider" })
                    .comment("The pane is designed for the Rider width. Phone fits a narrow pane; desktop needs a wide one.")
            }
            row {
                comment("Changes apply when the Workbench pane is next opened or reloaded.")
            }
        }
    }
}
