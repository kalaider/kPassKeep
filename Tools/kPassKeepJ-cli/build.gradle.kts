import org.graalvm.buildtools.gradle.tasks.BuildNativeImageTask
import org.gradle.nativeplatform.platform.internal.DefaultNativePlatform

plugins {
    java
    application
    id("org.graalvm.buildtools.native") version "0.9.4"
    id("io.freefair.lombok") version "6.4.1"
}

group = "org.kalaider.passkeep"
version = "1.0.0"

repositories {
    mavenCentral()
}

val generateConfig = configurations.register("generateConfig").get()

dependencies {
    implementation("info.picocli:picocli:4.6.3")
    implementation("com.fasterxml.jackson.core:jackson-databind:2.13.1")
    implementation("com.fasterxml.jackson.dataformat:jackson-dataformat-xml:2.13.1")
    implementation("org.yaml:snakeyaml:1.30")
    generateConfig("info.picocli:picocli-codegen:4.6.3")
}

application {
    mainClass.set("org.kalaider.passkeep.Tool")
}

tasks {
    val picocliReflectionConfigFile = "${buildDir}/native/generated/picocli/reflect-config.json"
    val graalConfigDirectory = "${projectDir}/src/main/resources/graalvm"

    val generateGraalReflectionConfig = register<JavaExec>("generateGraalReflectionConfig") {
        dependsOn.add(classes)
        mainClass.set("picocli.codegen.aot.graalvm.ReflectionConfigGenerator")
        classpath = generateConfig + sourceSets.main.get().runtimeClasspath
        args = listOf("--output=$picocliReflectionConfigFile", application.mainClass.get())
    }.get()

    named<BuildNativeImageTask>("nativeBuild") {
        dependsOn.add(generateGraalReflectionConfig)
    }

    nativeBuild {
        useFatJar.set(true)
        configurationFileDirectories.from(
            file(picocliReflectionConfigFile).parentFile,
            file(graalConfigDirectory)
        )
        buildArgs.add("-H:+TraceNativeToolUsage")
        // https://github.com/oracle/graal/issues/4072
        if (DefaultNativePlatform.getCurrentOperatingSystem().isWindows) {
            buildArgs.addAll(
                listOf(
                    "JDK_LoadSystemLibrary",
                    "JNU_CallMethodByName",
                    "JNU_CallMethodByNameV",
                    "JNU_CallStaticMethodByName",
                    "JNU_ClassString",
                    "JNU_GetEnv",
                    "JNU_GetFieldByName",
                    "JNU_GetStaticFieldByName",
                    "JNU_IsInstanceOfByName",
                    "JNU_NewObjectByName",
                    "JNU_NewStringPlatform",
                    "JNU_SetFieldByName",
                    "JNU_ThrowArrayIndexOutOfBoundsException",
                    "JNU_ThrowByName",
                    "JNU_ThrowIOException",
                    "JNU_ThrowIllegalArgumentException",
                    "JNU_ThrowInternalError",
                    "JNU_ThrowNullPointerException",
                    "JNU_ThrowOutOfMemoryError"
                ).map { "-H:NativeLinkerOption=/export:$it" }
            )
        }
        buildArgs.add("--report-unsupported-elements-at-runtime")
    }

    // emit distributions without version suffix to simplify CI scripts
    for (task in listOf(distTar, distZip)) {
        task.get().archiveVersion.set("")
    }
}
