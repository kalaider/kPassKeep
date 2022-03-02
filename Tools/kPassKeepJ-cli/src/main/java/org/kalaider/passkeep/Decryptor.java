package org.kalaider.passkeep;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonSubTypes;
import com.fasterxml.jackson.annotation.JsonTypeInfo;
import com.fasterxml.jackson.databind.ObjectMapper;
import lombok.SneakyThrows;

import javax.crypto.Cipher;
import javax.crypto.SecretKeyFactory;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.PBEKeySpec;
import javax.crypto.spec.SecretKeySpec;
import javax.security.sasl.AuthenticationException;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.security.Key;
import java.util.Base64;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.stream.Collectors;

public class Decryptor {
    public record EncryptedGroup(
            String version,
            String name,
            @JsonProperty("descr")
            String description,
            @JsonProperty("pass")
            String password,
            String salt,
            @JsonProperty("datasalt")
            String dataSalt,
            @JsonProperty("members")
            List<EncryptedEntry> entries
    ) {}

    public record EncryptedEntry(
            @JsonProperty("Data")
            String data,
            @JsonProperty("Guid")
            UUID guid
    ) {}

    public record Group(
            String version,
            String name,
            String description,
            Map<UUID, Entry> entries
    ) {}

    @JsonSubTypes({
            @JsonSubTypes.Type(value = Login.class, name = "login"),
            @JsonSubTypes.Type(value = Target.class, name = "target"),
            @JsonSubTypes.Type(value = Account.class, name = "account")
    })
    @JsonTypeInfo(use = JsonTypeInfo.Id.NAME,  property = "type")
    @JsonIgnoreProperties(ignoreUnknown = true)
    public sealed interface Entry {
        String description();
    }

    public record Login(
            String user,
            @JsonProperty("descr")
            String description
    ) implements Entry { }

    public record Target(
            String title,
            String uri,
            byte[] icon,
            @JsonProperty("descr")
            String description
    ) implements Entry { }

    public record Account(
            @JsonProperty("pass")
            String password,
            @JsonProperty("login")
            UUID loginRef,
            @JsonProperty("target")
            UUID targetRef,
            @JsonProperty("descr")
            String description
    ) implements Entry { }

    @SneakyThrows
    public Group decrypt(String groupJson, char[] password) {
        var group = new ObjectMapper().readValue(groupJson, EncryptedGroup.class);
        verifyPassword(group, password);
        var key = deriveKey(group, password);
        return new Group(
                group.version(),
                group.name(),
                group.description(),
                group.entries().stream()
                        .collect(Collectors.toMap(EncryptedEntry::guid, it -> decryptSingleAsEntry(it, key)))
        );
    }

    @SneakyThrows
    private Entry decryptSingleAsEntry(EncryptedEntry member, byte[] key) {
        return new ObjectMapper().readValue(decryptSingle(member, key), Entry.class);
    }

    @SneakyThrows
    private String decryptSingle(EncryptedEntry member, byte[] key) {
        var data = ByteBuffer.wrap(Base64.getDecoder().decode(member.data()));
        var iv = new byte[16];
        data.get(iv);
        Cipher cipher = Cipher.getInstance("AES/CBC/PKCS5Padding");
        cipher.init(Cipher.DECRYPT_MODE, new SecretKeySpec(key, "AES"), new IvParameterSpec(iv));
        return new String(cipher.doFinal(data.array(), data.position(), data.remaining()), StandardCharsets.UTF_8);
    }

    private byte[] deriveKey(EncryptedGroup group, char[] password) {
        return hash(password, Base64.getDecoder().decode(group.dataSalt()));
    }

    @SneakyThrows
    private void verifyPassword(EncryptedGroup group, char[] password) {
        var hash = hash(password, Base64.getDecoder().decode(group.salt));
        if (!Base64.getEncoder().encodeToString(hash).equals(group.password())) {
            throw new AuthenticationException();
        }
    }

    @SneakyThrows
    private byte[] hash(char[] password, byte[] salt) {
        SecretKeyFactory factory = SecretKeyFactory.getInstance("PBKDF2WithHmacSHA1");
        PBEKeySpec pbeKeySpec = new PBEKeySpec(password, salt, 500_000, 384);
        Key secretKey = factory.generateSecret(pbeKeySpec);
        var key = new byte[32];
        System.arraycopy(secretKey.getEncoded(), 0, key, 0, 32);
        return key;
    }
}
