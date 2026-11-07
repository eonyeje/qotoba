package qotoba.workbench.rider

import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import java.io.File

/**
 * Where the Workbench service is and how the pane proves who it is.
 * The token is read, in order, from the override typed here, the QOTOBA_WB_TOKEN variable, and the token file
 * the service writes beside its database.
 */
@Service(Service.Level.APP)
@State(name = "QotobaWorkbench", storages = [Storage("qotobaWorkbench.xml")])
class WorkbenchSettings : PersistentStateComponent<WorkbenchSettings.State> {

    class State {
        var serviceUrl: String = "http://localhost:5290"
        var tokenOverride: String = ""
        var tokenPath: String = ""
        var layout: String = "auto"
    }

    private var state = State()

    override fun getState(): State = state
    override fun loadState(state: State) { this.state = state }

    val serviceUrl: String get() = state.serviceUrl.trimEnd('/')
    val layout: String get() = state.layout.ifBlank { "auto" }

    fun resolveToken(): String? {
        state.tokenOverride.trim().takeIf { it.isNotEmpty() }?.let { return it }
        System.getenv("QOTOBA_WB_TOKEN")?.trim()?.takeIf { it.isNotEmpty() }?.let { return it }
        val path = state.tokenPath.trim().ifEmpty { File(System.getProperty("user.home"), ".qotoba-workbench/token").path }
        val f = File(path)
        return if (f.isFile) f.readText().trim().takeIf { it.isNotEmpty() } else null
    }

    /** The page address: embedded (fills the pane, layout follows its width), a fixed layout only if chosen, the token once. */
    fun pageUrl(route: String = "#/inbox"): String {
        val token = resolveToken()
        val q = buildString {
            append("?embed=1")
            if (layout != "auto") append("&layout=").append(layout)
            if (token != null) append("&token=").append(token)
        }
        return "$serviceUrl/$q$route"
    }

    companion object {
        fun instance(): WorkbenchSettings = ApplicationManager.getApplication().getService(WorkbenchSettings::class.java)
    }
}
