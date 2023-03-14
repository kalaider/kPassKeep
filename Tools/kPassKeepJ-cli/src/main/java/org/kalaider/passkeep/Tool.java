package org.kalaider.passkeep;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.dataformat.xml.XmlMapper;
import lombok.SneakyThrows;
import org.yaml.snakeyaml.DumperOptions;
import org.yaml.snakeyaml.Yaml;
import picocli.CommandLine;

import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;

import static picocli.CommandLine.*;

@Command(
        mixinStandardHelpOptions = true,
        description = "kPassKeep CLI",
        name = "kPassKeepJ-cli",
        subcommands = Tool.Decrypt.class
)
public class Tool {
    public enum Format {
        YAML, JSON, XML
    }

    public enum Style {
        RAW(it -> it),
        PRETTY(new PrettyFormatter()),
        KEEPASS_V2(new KeePassFormatter());

        private final Formatter formatter;

        public Formatter getFormatter() {
            return formatter;
        }

        Style(Formatter formatter) {
            this.formatter = formatter;
        }
    }

    @Command(
            mixinStandardHelpOptions = true,
            description = "Decrypts group file",
            name = "decrypt"
    )
    public static class Decrypt implements Runnable {
        @Parameters(paramLabel = "FILE", description = "Group file")
        private Path groupFilePath;
        @Option(names = {"-s", "--style"}, description = "Output style")
        private Style style = Style.PRETTY;
        @Option(names = {"-f", "--format"}, description = "Output format")
        private Format format = Format.YAML;
        @Option(names = {"-c", "--charset"}, description = "Charset of the output")
        private Charset charset = StandardCharsets.UTF_8;
        @Parameters(paramLabel = "PASSWORD", description = "Passphrase", prompt = "Group password, please: ", interactive = true)
        private char[] password;
        @Option(names = {"-o", "--output"}, description = "Output decrypted contents to specific file")
        private Path outputFilePath;

        public static record Root<T>(T value) {}

        @Override
        @SneakyThrows
        public void run() {
            var json = Files.readString(groupFilePath, StandardCharsets.UTF_8);
            // trim BOM
            if (json.startsWith("\ufeff")) {
                json = json.substring(1);
            }
            var decrypted = new Decryptor().decrypt(json, password);
            var formatted = style.getFormatter().format(decrypted);
            var text = switch (format) {
                case JSON -> new ObjectMapper().writerWithDefaultPrettyPrinter().writeValueAsString(formatted);
                case YAML -> new Yaml(dumperOptions())
                        .dump(new ObjectMapper().convertValue(formatted, Object.class));
                case XML -> new XmlMapper()
                        .writerWithDefaultPrettyPrinter()
                        .writeValueAsString(formatted);
            };
            if (outputFilePath == null) {
                System.out.write(text.getBytes(charset));
            } else {
                Files.writeString(outputFilePath, text, charset);
            }
        }

        private DumperOptions dumperOptions() {
            var options = new DumperOptions();
            options.setPrettyFlow(true);
            options.setDefaultFlowStyle(DumperOptions.FlowStyle.BLOCK);
            options.setIndentWithIndicator(true);
            options.setAllowUnicode(true);
            options.setIndent(2);
            return options;
        }
    }

    public static void main(String[] args) {
        System.exit(
            new CommandLine(new Tool()).execute(args)
        );
    }
}
