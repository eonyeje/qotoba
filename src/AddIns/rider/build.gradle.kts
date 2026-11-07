import org.jetbrains.kotlin.gradle.dsl.JvmTarget

plugins {
    id("java")
    id("org.jetbrains.kotlin.jvm") version "2.4.21"
    id("org.jetbrains.intellij.platform") version "2.20.0"
}

group = "qotoba.workbench"
version = providers.gradleProperty("pluginVersion").get()

repositories {
    mavenCentral()
    intellijPlatform { defaultRepositories() }
}

dependencies {
    // built against the Rider installed on this machine, so nothing is downloaded for the platform
    intellijPlatform {
        local(providers.gradleProperty("riderHome").get())
        // the embedded browser is a bundled plugin in 2026.x, not part of the runtime
        bundledPlugin("com.intellij.modules.jcef")
    }
}

intellijPlatform {
    pluginConfiguration {
        id = "qotoba.workbench.rider"
        name = "Qotoba Workbench"
        version = project.version.toString()
        ideaVersion {
            sinceBuild = "262"
            untilBuild = provider { null }
        }
    }
    buildSearchableOptions = false
    instrumentCode = false
}

java {
    sourceCompatibility = JavaVersion.VERSION_21
    targetCompatibility = JavaVersion.VERSION_21
}

tasks.withType<org.jetbrains.kotlin.gradle.tasks.KotlinCompile>().configureEach {
    compilerOptions { jvmTarget.set(JvmTarget.JVM_21) }
}

tasks.withType<JavaCompile>().configureEach { options.release.set(21) }
