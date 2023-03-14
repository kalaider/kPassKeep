package org.kalaider.passkeep;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.core.JsonGenerator;
import com.fasterxml.jackson.databind.JsonSerializer;
import com.fasterxml.jackson.databind.SerializerProvider;
import com.fasterxml.jackson.databind.annotation.JsonSerialize;
import com.fasterxml.jackson.dataformat.xml.annotation.JacksonXmlElementWrapper;
import com.fasterxml.jackson.dataformat.xml.annotation.JacksonXmlRootElement;

import java.io.IOException;
import java.nio.ByteBuffer;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

public class KeePassFormatter implements Formatter {
    @JsonInclude(JsonInclude.Include.NON_EMPTY)
    @JacksonXmlRootElement(localName = "KeePassFile")
    public record Database(
            @JsonProperty("Meta")
            Meta meta,
            @JsonProperty("Group")
            @JacksonXmlElementWrapper(localName = "Root")
            List<Root> root
    ) { }

    @JsonInclude(JsonInclude.Include.NON_EMPTY)
    public record Root(
            @JsonProperty("Name")
            String name,
            @JsonProperty("Group")
            Group group
    ) { }

    @JsonInclude(JsonInclude.Include.NON_EMPTY)
    public record Meta(
            @JsonProperty("Icon")
            @JacksonXmlElementWrapper(localName = "CustomIcons")
            List<Icon> customIcons
    ) { }

    @JsonInclude(JsonInclude.Include.NON_EMPTY)
    public record Icon(
            @JsonProperty("UUID")
            @JsonSerialize(using = BinaryUuidSerializer.class)
            UUID uuid,
            @JsonProperty("Data")
            byte[] data
    ) { }

    @JsonInclude(JsonInclude.Include.NON_EMPTY)
    public record Group(
            @JsonProperty("Name")
            String name,
            @JsonProperty("Entry")
            @JacksonXmlElementWrapper(useWrapping = false)
            List<Entry> entries
    ) { }

    @JsonInclude(JsonInclude.Include.NON_EMPTY)
    public record Entry(
            @JsonProperty("CustomIconUUID")
            @JsonSerialize(using = BinaryUuidSerializer.class)
            UUID customIconUuid,
            @JsonProperty("String")
            @JacksonXmlElementWrapper(useWrapping = false)
            List<StringField> fields
    ) { }

    public record StringField(
            @JsonProperty("Key")
            String key,
            @JsonProperty("Value")
            String value
    ) { }

    public Object format(Decryptor.Group group) {
        var meta = new Meta(group.entries().entrySet().stream()
                .filter(it -> it.getValue() instanceof Decryptor.Target)
                .map(it -> new Icon(it.getKey(), ((Decryptor.Target)it.getValue()).icon()))
                .toList());
        var entries = group.entries().values().stream()
                .filter(Decryptor.Account.class::isInstance)
                .map(Decryptor.Account.class::cast)
                .map(acc -> new Entry(acc.targetRef(), List.of(
                        new StringField("Title", Optional.ofNullable((Decryptor.Target) group.entries().get(acc.targetRef()))
                                .map(Decryptor.Target::title).orElse(null)),
                        new StringField("URL", Optional.ofNullable((Decryptor.Target) group.entries().get(acc.targetRef()))
                                .map(Decryptor.Target::uri).orElse(null)),
                        new StringField("UserName", Optional.ofNullable((Decryptor.Login) group.entries().get(acc.loginRef()))
                                .map(Decryptor.Login::user).orElse(null)),
                        new StringField("Password", acc.password()),
                        new StringField("Notes", acc.description())
                )))
                .toList();
        return new Database(meta, List.of(new Root("kPassKeep", new Group(group.name(), entries))));
    }

    public static class BinaryUuidSerializer extends JsonSerializer<UUID> {
        @Override
        public void serialize(UUID value, JsonGenerator gen, SerializerProvider serializers) throws IOException {
            serializers.defaultSerializeValue(toBytes(value), gen);
        }

        private byte[] toBytes(UUID uuid) {
            ByteBuffer bb = ByteBuffer.wrap(new byte[16]);
            bb.putLong(uuid.getMostSignificantBits());
            bb.putLong(uuid.getLeastSignificantBits());
            return bb.array();
        }
    }
}
