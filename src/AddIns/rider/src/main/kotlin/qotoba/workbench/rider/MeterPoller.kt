package qotoba.workbench.rider

import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.wm.ToolWindow
import com.intellij.util.concurrency.AppExecutorUtil
import java.net.URI
import java.net.http.HttpClient
import java.net.http.HttpRequest
import java.net.http.HttpResponse
import java.time.Duration
import java.util.concurrent.ScheduledFuture
import java.util.concurrent.TimeUnit

/**
 * Asks the service every 30 seconds how many cards wait and shows it in the tool window title:
 * "Workbench · 3 waiting · ~2 min". Nothing else is polled; the page itself listens to the service's events.
 */
class MeterPoller(private val toolWindow: ToolWindow) {

    private val http: HttpClient = HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(3)).build()
    private var task: ScheduledFuture<*>? = null

    fun start() {
        task = AppExecutorUtil.getAppScheduledExecutorService().scheduleWithFixedDelay({ poll() }, 2, 30, TimeUnit.SECONDS)
        Disposer.register(toolWindow.disposable) { task?.cancel(false) }
    }

    private fun poll() {
        val s = WorkbenchSettings.instance()
        val token = s.resolveToken() ?: return setTitle("Workbench · no token")
        val req = HttpRequest.newBuilder(URI.create(s.serviceUrl + "/api/meter"))
            .header("Authorization", "Bearer $token").timeout(Duration.ofSeconds(5)).GET().build()
        val title = try {
            val res = http.send(req, HttpResponse.BodyHandlers.ofString())
            if (res.statusCode() != 200) "Workbench · service answered ${res.statusCode()}" else {
                val waiting = Regex("\"waiting\":(\\d+)").find(res.body())?.groupValues?.get(1)?.toInt() ?: 0
                val minutes = Regex("\"minutes\":(\\d+)").find(res.body())?.groupValues?.get(1)?.toInt() ?: 0
                if (waiting == 0) "Workbench · nothing waits on you" else "Workbench · $waiting waiting · ~$minutes min"
            }
        } catch (e: Exception) { "Workbench · service not reachable" }
        setTitle(title)
    }

    private fun setTitle(title: String) {
        ApplicationManager.getApplication().invokeLater({ if (!toolWindow.isDisposed) toolWindow.title = title.removePrefix("Workbench · ") }, { toolWindow.isDisposed })
    }
}
