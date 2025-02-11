#!/usr/bin/env sh
# arguments
# $1 = build version
# $2 = project path
# $3 = xcode path
# $4 = code sign type (all/adhoc/inhouse)
# $5 = is generate archive

echo "Arguments = $@ (count = $#)"

######################################## VALUES ########################################
MAIN_PATH="$HOME/Documents/First_Hero"
XCODE_FINDER_BASE_PATH="$HOME/Documents/Chosen_Heroes_Xcode"

# arguments value set
if [ $# -gt 0 ]; then
    BUILD_VERSION="$1"
else
    BUILD_VERSION="first_hero_v1.0.0_b0_r7"
fi

if [ $# -gt 1 ]; then
    PROJECT_PATH="$2"
else
    CURR_DIR="$(dirname $0)"
    PROJECT_ROOT_PATH="${CURR_DIR#$MAIN_PATH/}"
    PROJECT_PATH="$MAIN_PATH/${PROJECT_ROOT_PATH%%/*}"
fi

if [ $# -gt 2 ]; then
    XCODE_FINDER_PATH="$3"
else
    XCODE_FINDER_PATH="${XCODE_FINDER_BASE_PATH}/$BUILD_VERSION"
fi

if [ $# -gt 3 ]; then
    BUILD_KIND="$4"
else
    BUILD_KINDd="Dev"
fi

if [ $# -gt 4 ]; then
    CODE_SIGN_TYPE="$5"
else
    CODE_SIGN_TYPE="all"
fi

if [ $# -gt 5 ]; then
    IS_GENERATE_ARCHIVE="$6"
else
    IS_GENERATE_ARCHIVE="True"
fi

# path
XCODE_PATH="$XCODE_FINDER_PATH/Unity-iPhone.xcodeproj"
XCODE_XCWORKSPACE_PATH="$XCODE_FINDER_PATH/Unity-iPhone.xcworkspace"
APP_FINDER_PATH="$XCODE_FINDER_PATH/build"
ARCHIVE_FINDER_PATH="$HOME/Library/Developer/Xcode/Archives/Chosen_Heroes/$BUILD_VERSION"
IPA_FINDER_PATH="$MAIN_PATH/release/iOS_Arabic/$BUILD_KIND/$BUILD_VERSION"

# signing path
SIGNING_DATA_FINDER_PATH="$PROJECT_PATH/GameData/iOS"
XCCONFIG_FINDER_PATH="$SIGNING_DATA_FINDER_PATH/xcconfig"
PLIST_FINDER_PATH="$SIGNING_DATA_FINDER_PATH/plist"

PROVISION_PROFILE_DEVELOPMENT="development"
PROVISION_PROFILE_ADHOC="adhoc"
PROVISION_PROFILE_APPSTORE="appstore"
PROVISION_PROFILE_INHOUSE="inhouse"

######################################## FUNCTIONS ########################################
# notification message
notification_message()
{
    $HOME/Documents/shell_script/DisplayNotification.sh "${1}" "${2}"
}

# get uuid function
get_uuid()
{
    PROVISION_PROFILE_UUID=`/usr/libexec/PlistBuddy -c 'Print :UUID' /dev/stdin <<< $(security cms -D -i "${1}")`
}

# generate archive function
generate_archive()
{
    if [ ${IS_GENERATE_ARCHIVE} == "True" ]; then
    {
        # delete exist archive
        if [ -e "$ARCHIVE_FINDER_PATH" ]; then
            rm -rf "$ARCHIVE_FINDER_PATH"
        fi

        if [ -e "$XCODE_XCWORKSPACE_PATH" ]; then
        {
            # generate xcworkspace archive
            xcodebuild -workspace "$XCODE_XCWORKSPACE_PATH" -scheme Unity-iPhone archive -archivePath "$ARCHIVE_FINDER_PATH/${1}.xcarchive" -xcconfig "$XCCONFIG_FINDER_PATH/${2}.xcconfig" CONFIGURATION_BUILD_DIR=$APP_FINDER_PATH/
        }
        else
        {
            # generate xcodeproj archive
            xcodebuild -project "$XCODE_PATH" -scheme Unity-iPhone archive -archivePath "$ARCHIVE_FINDER_PATH/${1}.xcarchive" -xcconfig "$XCCONFIG_FINDER_PATH/${2}.xcconfig" CONFIGURATION_BUILD_DIR=$APP_FINDER_PATH/
        }
        fi

        if [ -d "$ARCHIVE_FINDER_PATH" ]; then
            notification_message "Generate Archive Successful" "${1}.xcarchive"
        fi
    }
    fi
}

# generate ipa function
generate_ipa()
{
    local IPA_PATH="$IPA_FINDER_PATH"
    local IPA_FILE_NAME="$IPA_PATH/${BUILD_VERSION}_${2}.ipa"

    # delete exist ipa
    if [ -e "$IPA_FILE_NAME" ]; then
        rm -rf "$IPA_FILE_NAME"
    fi

    # generate ipa
    xcodebuild -exportArchive -archivePath "$ARCHIVE_FINDER_PATH/${1}.xcarchive" -exportPath "$IPA_PATH" -exportOptionsPlist "$PLIST_FINDER_PATH/${2}.plist"

    if [ -d "$IPA_PATH" ]; then
    {
        # delete files except ipa
        for file in `ls "$IPA_PATH"`
        do
        {
            if [[ $file != *".ipa"* ]]; then
                rm -rf "$IPA_PATH/$file"
            fi
        }
        done

        # chagne ipa name
        mv "$IPA_PATH/Unity-iPhone.ipa" "$IPA_FILE_NAME"
    }
    fi
}

######################################## PROCESS ########################################
# check project path
if [ ! -d "$PROJECT_PATH" ]; then
    echo "Error, No project path"
    exit 1
fi

# check xcode path
if [ ! -d "$XCODE_FINDER_PATH" ] || [ ! -x "$XCODE_PATH" ]; then
    echo "Error, No xcode path"
    exit 1
fi

# unlock keychain to ignore code sign error
security list-keychains
security unlock-keychain  -p "maxonsoft" ~/Library/Keychains/login.keychain

# generate archive
if [ ${CODE_SIGN_TYPE} == "adhoc" ] || [ ${CODE_SIGN_TYPE} == "all" ]; then
    generate_archive "$BUILD_VERSION" "adhoc_sign_identity"
fi

# generate ipa
if [ ${CODE_SIGN_TYPE} == "adhoc" ] || [ ${CODE_SIGN_TYPE} == "all" ]; then
    {
    # develpoment ipa
    #generate_ipa "$BUILD_VERSION" "$PROVISION_PROFILE_DEVELOPMENT"

    # adhoc ipa
    generate_ipa "$BUILD_VERSION" "$PROVISION_PROFILE_ADHOC"

    # appstore ipa
    generate_ipa "$BUILD_VERSION" "$PROVISION_PROFILE_APPSTORE"
}
fi

# generate archive
if [ ${CODE_SIGN_TYPE} == "inhouse" ] || [ ${CODE_SIGN_TYPE} == "all" ]; then
    generate_archive "${BUILD_VERSION}_$PROVISION_PROFILE_INHOUSE" "inhouse_sign_identity"
fi

# generate ipa
if [ ${CODE_SIGN_TYPE} == "inhouse" ] || [ ${CODE_SIGN_TYPE} == "all" ]; then
    # inhouse ipa
    generate_ipa "${BUILD_VERSION}_$PROVISION_PROFILE_INHOUSE" "$PROVISION_PROFILE_INHOUSE"
fi

# open ipa path finder
if [ -d "$IPA_FINDER_PATH" ]; then
{
    notification_message "iOS Build Generator Successful"
    open "$IPA_FINDER_PATH"
}
fi
