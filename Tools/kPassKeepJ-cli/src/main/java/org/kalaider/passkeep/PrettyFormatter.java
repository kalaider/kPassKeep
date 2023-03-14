package org.kalaider.passkeep;

import com.fasterxml.jackson.annotation.JsonInclude;

import java.util.List;

public class PrettyFormatter implements Formatter {

    @JsonInclude(JsonInclude.Include.NON_EMPTY)
    public record Group(
            String name,
            String description,
            List<Account> accounts
    ) {}

    @JsonInclude(JsonInclude.Include.NON_EMPTY)
    public record Login(
            String user,
            String description
    ) { }

    @JsonInclude(JsonInclude.Include.NON_EMPTY)
    public record Target(
            String title,
            String uri,
            String description
    ) { }

    @JsonInclude(JsonInclude.Include.NON_EMPTY)
    public record Account(
            String password,
            Login login,
            Target target,
            String description
    ) { }

    public Object format(Decryptor.Group group) {
        return new Group(
                group.name(),
                group.description(),
                group.entries().values().stream()
                        .filter(Decryptor.Account.class::isInstance)
                        .map(Decryptor.Account.class::cast)
                        .map(account -> format(group, account))
                        .toList()
        );
    }

    private Account format(Decryptor.Group group, Decryptor.Account account) {
        return new Account(
                account.password(),
                format((Decryptor.Login) group.entries().get(account.loginRef())),
                format((Decryptor.Target) group.entries().get(account.targetRef())),
                group.description()
        );
    }

    private Login format(Decryptor.Login login) {
        return new Login(
                login.user(),
                login.description()
        );
    }

    private Target format(Decryptor.Target target) {
        return new Target(
                target.title(),
                target.uri(),
                target.description()
        );
    }
}
