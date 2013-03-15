![SkyFloe](https://raw.github.com/bspell1/SkyFloe/master/SkyFloe.png) SkyFloe
=============================================================================

A backup system for low cost, long term, encrypted cloud backup over storage services such as [Amazon Glacier](http://aws.amazon.com/glacier/). Download the latest binaries [here](http://brentspell.com/download/skyfloe.zip). Install on Debian/Ubuntu using the source

    deb [arch=all] http://brentspell.com/apt/ stable main

and run

    sudo apt-get update
    sudo mozroots --import --ask-remove
    curl http://brentspell.com/apt/public.key | sudo apt-key add -
    sudo apt-get install skyfloe

##Features##
* Pluggable backup components, providing the choice of cloud storage provider, or just a file system
* Client-side password encryption, so the storage provider cannot access archived files
* Minimal intermediate storage overhead, streaming files to the storage provider on the fly
* Fault tolerance, for recovering from transient network faults and skipping inaccessible files
* Incremental backup, adding only new/changed/deleted files since the previous completed backup session
* Backup/restore include/exclude path filtering
* Cross-platform, running under [Mono](http://mono-project.com)

##Backup##

    skyfloe-backup                                                      \
        -connect "Store=AwsGlacier;AccessKey=...;SecretKey=...;"        \
        -archive MyArchive                                              \
        -password secret                                                \
        -source ~/myfiles

This command would upload all files within `~/myfiles` to AWS Glacier, into a vault named `SkyFloe-MyArchive` encrypted with the password "secret." The backup may be paused (and later resumed) at any time by pressing `{escape}`.

    -c, -connect {connection-string}

Identifies the backup store type and the parameters needed to connect to the backup store. The example above specifies the AWS [access key and secret key](https://portal.aws.amazon.com/gp/aws/securityCredentials) used to connect to Amazon Glacier. For the file system store, a connection string would be the following: `Store=FileSystem;Path=...`

    -a, -archive {archive-name}

Specifies the name of the archive to create/extend. Archive names are case insensitive and must consist of letters and numbers only.

    -p, -password {password}

Indicates the password for the archive. For new archives, this value establishes the archive password for file encryption. For existing archives (to extend), this value must match the password used to create the archive.

    -r, -max-retry {retry-count}
    -f, -max-fail {failure-count}

The maximum consecutive retry and failure count. If an error occurs while backing up a file, the operation will be retried for up to `{retry-count}` failures. After that, the source file is skipped, the retry count is reset, and the failure count is incremented. If `{failure-count}` is exceeded with no successful file backup, the entire session is aborted. Both counters are reset after a successful file backup.

    -s, -source {path}

Specifies a source directory to add to the backup. All descendant files within that directory (that match the filter) are added to the archive. Multiple sources may be specified in a single backup session.

    -n, -include {regex}
    -x, -exclude {regex}

Indicates the inclusion/exclusion filter regular expression to apply to all files. Filter expressions are applied to the full file path. If there are any inclusion filters, then only those files that match at least one of the filter expressions (and aren't excluded) will be added to the archive. Any file path that matches an exclusion filter is excluded, regardless of whether it matches an inclusion filter.

    -k, -delete[+/-]

If true (-delete or -delete+), the backup archive is deleted before starting the session. This ensures that a new archive will be created during the backup.

    -d, -diff {diff-method}

Specifies a differencing method to use when extending an existing archive. If "timestamp" (the default), then file last write times are used to determine whether a file changed since it was backed up. If "digest," then file CRC values are compared.

    -t, -checkpoint {checkpoint-size}

Indicates the checkpoint size for the backup, in MB (default = 1GB). Checkpoints are the unit of crash/cancellation recovery in SkyFloe. The backup will force a checkpoint after backing up `{checkpoint-size}` bytes. During the checkpoint, all blob and index changes are flushed to the storage provider.

    -l, -rate {rate-limit}

The backup rate limit. By default, files are backed up as fast as they can be written to the storage provider. To minimize CPU/network utilization, specify a rate limit (in KB/sec) for the backup operation.

    -z, -compress[+/-]

Specifies whether to compress files during the backup using [LZ4](http://code.google.com/p/lz4/). By default, files are not compressed.

##Restore##

    skyfloe-restore                                                     \
        -connect "Store=FileSystem;Path=/mnt/backup/;"                  \
        -archive MyArchive                                              \
        -password wazzup

This command would restore the latest versions of all files in the MyArchive archive in the file system storage provider to their original locations.

    -c, -connect {connection-string}
    -a, -archive {archive-name}
    -p, -password {password}
    -r, -max-retry {retry-count}
    -f, -max-fail {failure-count}
    -l, -rate {rate-limit}

These options are the same as for backup.

    -n, -include {regex}
    -x, -exclude {regex}

The filter options are the same as for backup. Note that the expressions are applied to each file's original full backup path.

    -m, -map-path {source}={target}

Specifies a root path mapping. `{source}` must match an original path passed into the backup using the `-source` parameter. `{target}` specifies the path to the directory to which to restore the files that were backed up from that source. A path mapping may be specified for each source root path. By default, files are restored to their original locations.

    -i, -file {path}

Specifies a single subdirectory/file to extract from the archive. `{file-path}` must be a full source path. This parameter may be specified multiple times to select a subset of the archive for restore.

    -e, -skip-existing[+/-]

Indicates that the backup should only restore files that do not exist at the destination. By default, existing files are overwritten.

    -o, -skip-readonly[+/-]

Indicates that the backup should ignore read-only files at the destination. By default, read-only files are marked writable and overwritten. This value is ignored if `skip-existing` is true.

    -v, -verify[+/-]

Specifies that the restore should verify file CRCs after restoring them. Mismatched CRCs will trigger retries/failures.

    -k, -delete[+/-]

Indicates that the restore should process delete records included in the backup. By default, the restore leaves files at the destination that were marked as deleted in the backup.

    -u, -cleanup[+/-]

Indicates that the restore should clean up the existing restore history for the archive. By default, restore history is preserved locally in `%LOCALAPPDATA%/SkyFloe/{provider}/{archive}` (Windows).

##Build Notes##
* If you are building in Visual Studio, you will need `tar` and `gzip` in your path to build the Linux APT package project (or remove that project).
