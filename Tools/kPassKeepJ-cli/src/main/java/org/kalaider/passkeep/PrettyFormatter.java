package org.kalaider.passkeep;

import com.fasterxml.jackson.annotation.JsonInclude;

import java.util.List;

public class PrettyFormatter {

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

    public Group prettify(Decryptor.Group group) {
        return new Group(
                group.name(),
                group.description(),
                group.entries().values().stream()
                        .filter(Decryptor.Account.class::isInstance)
                        .map(Decryptor.Account.class::cast)
                        .map(account -> prettify(group, account))
                        .toList()
        );
    }

    private Account prettify(Decryptor.Group group, Decryptor.Account account) {
        return new Account(
                account.password(),
                prettify((Decryptor.Login) group.entries().get(account.loginRef())),
                prettify((Decryptor.Target) group.entries().get(account.targetRef())),
                group.description()
        );
    }

    private Login prettify(Decryptor.Login login) {
        return new Login(
                login.user(),
                login.description()
        );
    }

    private Target prettify(Decryptor.Target target) {
        return new Target(
                target.title(),
                target.uri(),
                target.description()
        );
    }
}
