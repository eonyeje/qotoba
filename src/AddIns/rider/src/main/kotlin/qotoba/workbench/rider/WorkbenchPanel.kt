package qotoba.workbench.rider

import com.intellij.icons.AllIcons
import com.intellij.ide.BrowserUtil
import com.intellij.openapi.util.IconLoader
import com.intellij.openapi.Disposable
import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.options.ShowSettingsUtil
import com.intellij.openapi.project.Project
import com.intellij.openapi.project.guessProjectDir
import com.intellij.openapi.roots.ProjectRootManager
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.vfs.LocalFileSystem
import com.intellij.ui.components.JBLabel
import com.intellij.ui.jcef.JBCefApp
import com.intellij.ui.jcef.JBCefBrowser
import com.intellij.ui.jcef.JBCefBrowserBase
import com.intellij.ui.jcef.JBCefJSQuery
import com.intellij.util.ui.JBUI
import org.cef.browser.CefBrowser
import org.cef.browser.CefFrame
import org.cef.handler.CefLoadHandlerAdapter
import java.awt.BorderLayout
import java.io.File
import javax.swing.JComponent
import javax.swing.JPanel
import javax.swing.SwingConstants

/**
 * The Workbench page inside Rider. The page is the same one the browser shows; the pane adds a toolbar,
 * passes the token once, and gives the page a way to open a file at a line in this IDE.
 */
class WorkbenchPanel(private val project: Project, parent: Disposable) : JPanel(BorderLayout()), Disposable {

    private val browser: JBCefBrowser? = if (JBCefApp.isSupported()) JBCefBrowser() else null
    private var openQuery: JBCefJSQuery? = null

    init {
        Disposer.register(parent, this)
        add(toolbar(), BorderLayout.NORTH)
        if (browser == null) {
            add(fallback(), BorderLayout.CENTER)
        } else {
            Disposer.register(this, browser)
            installBridge(browser)
            add(browser.component, BorderLayout.CENTER)
            load("#/inbox")
        }
    }

    fun load(route: String) { browser?.loadURL(WorkbenchSettings.instance().pageUrl(route)) }

    private fun toolbar(): JComponent {
        val own = IconLoader.getIcon("/icons/workbench.svg", WorkbenchPanel::class.java)
        val group = DefaultActionGroup(
            action("Inbox", "The inbox is the only place you act", own) { load("#/inbox") },
            action("Back", "Go back", AllIcons.Actions.Back) { browser?.cefBrowser?.goBack() },
            action("Reload", "Reload the page", AllIcons.Actions.Refresh) { browser?.cefBrowser?.reload() },
            action("Open in browser", "Open the same page in your browser", AllIcons.Ide.External_link_arrow) { BrowserUtil.browse(WorkbenchSettings.instance().pageUrl().replace("?embed=1&", "?").replace("?embed=1", "")) },
            action("Settings", "Service address and token", AllIcons.General.Settings) { ShowSettingsUtil.getInstance().showSettingsDialog(project, WorkbenchConfigurable::class.java) }
        )
        val tb = ActionManager.getInstance().createActionToolbar("QotobaWorkbench", group, true)
        tb.targetComponent = this
        return tb.component
    }

    private fun action(text: String, description: String, icon: javax.swing.Icon, run: () -> Unit) = object : AnAction(text, description, icon) {
        override fun actionPerformed(e: AnActionEvent) = run()
    }

    private fun fallback(): JComponent {
        val label = JBLabel(
            "<html><body style='width:320px'><b>The embedded browser is off in this IDE.</b><br>" +
                "Turn on <i>ide.browser.jcef.enabled</i> in Registry, or use Open in browser.</body></html>",
            SwingConstants.CENTER
        )
        label.border = JBUI.Borders.empty(24)
        return label
    }

    /** window.QOTOBA_RIDER.open(path, line): the page asks, the IDE opens the file. */
    private fun installBridge(b: JBCefBrowser) {
        val q = JBCefJSQuery.create(b as JBCefBrowserBase)
        openQuery = q
        q.addHandler { payload ->
            val parts = payload.split('\u001f')
            val path = parts.getOrNull(0) ?: return@addHandler null
            val line = parts.getOrNull(1)?.toIntOrNull() ?: 1
            ApplicationManager.getApplication().invokeLater { openInEditor(path, line) }
            null
        }
        b.jbCefClient.addLoadHandler(object : CefLoadHandlerAdapter() {
            override fun onLoadEnd(cef: CefBrowser, frame: CefFrame, httpStatusCode: Int) {
                if (!frame.isMain) return
                val call = q.inject("payload")
                cef.executeJavaScript(
                    "window.QOTOBA_RIDER = { open: function (path, line) { var payload = String(path) + '\\u001f' + String(line || 1); $call } };",
                    cef.url, 0
                )
            }
        }, b.cefBrowser)
    }

    private fun openInEditor(path: String, line: Int) {
        val candidates = mutableListOf<File>()
        val f = File(path)
        if (f.isAbsolute) candidates.add(f)
        else {
            project.guessProjectDir()?.path?.let { candidates.add(File(it, path)) }
            ProjectRootManager.getInstance(project).contentRoots.forEach { candidates.add(File(it.path, path)) }
        }
        val hit = candidates.firstOrNull { it.isFile } ?: return
        val vf = LocalFileSystem.getInstance().refreshAndFindFileByIoFile(hit) ?: return
        OpenFileDescriptor(project, vf, (line - 1).coerceAtLeast(0), 0).navigate(true)
    }

    override fun dispose() {
        openQuery?.let { Disposer.dispose(it) }
    }
}
