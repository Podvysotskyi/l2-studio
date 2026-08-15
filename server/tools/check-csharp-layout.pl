#!/usr/bin/env perl

use strict;
use warnings;
use File::Basename qw(basename);
use File::Find qw(find);

my $repository = shift // '.';
my @roots = map { "$repository/$_" } qw(server/src server/tests);
my @errors;

for my $root (@roots) {
    die "C# source root was not found: $root\n" unless -d $root;
    find(
        {
            no_chdir => 1,
            wanted => sub {
                return unless -f $_ && /\.cs\z/;
                return if m{/(?:bin|obj)/};
                inspect_file($_);
            }
        },
        $root
    );
}

if (@errors) {
    print STDERR "C# file layout violations:\n";
    print STDERR "- $_\n" for sort @errors;
    exit 1;
}

print "C# file layout is valid.\n";

sub inspect_file {
    my ($path) = @_;
    my $file_name = basename($path, '.cs');

    return if $file_name =~ /\A(?:Program|GlobalUsings|AssemblyInfo)\z/;
    return if $path =~ m{/L2\.Studio\.Migrations/Migrations/};

    open my $file, '<', $path or die "Could not read $path: $!\n";
    my @declarations;
    while (my $line = <$file>) {
        next unless $line =~ /\A(?:public|internal)\s+/;

        if ($line =~ /\A(?:public|internal)\s+((?:(?:abstract|sealed|static|readonly|ref|unsafe|partial)\s+)*)
                     (?:class|interface|struct|enum|record(?:\s+(?:class|struct))?)\s+
                     ([A-Za-z_][A-Za-z0-9_]*)/x) {
            my $modifiers = $1;
            my $name = $2;
            push @declarations, {
                name => $name,
                partial => ($modifiers =~ /\bpartial\b/ ? 1 : 0)
            };
            next;
        }

        if ($line =~ /\A(?:public|internal)\s+.*\bdelegate\s+.*\b
                     ([A-Za-z_][A-Za-z0-9_]*)\s*(?:<[^>]+>)?\s*\(/x) {
            push @declarations, { name => $1, partial => 0 };
        }
    }
    close $file;

    if (@declarations != 1) {
        push @errors, "$path declares " . scalar(@declarations) .
            ' top-level types; expected exactly one';
        return;
    }

    my $declaration = $declarations[0];
    return if $file_name eq $declaration->{name};
    return if $declaration->{partial} &&
        $file_name =~ /\A\Q$declaration->{name}\E\.[A-Za-z0-9_.-]+\z/;

    push @errors, "$path declares $declaration->{name}; expected file " .
        "$declaration->{name}.cs" .
        ($declaration->{partial} ? ' or an intentional same-prefix partial file' : '');
}
