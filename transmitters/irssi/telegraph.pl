#!/usr/bin/env perl -w
use strict;
use Irssi;
use LWP::UserAgent; 
use HTTP::Request::Common qw(POST); 

my $VERSION = '1.00';
my %IRSSI = (
	authors		=>	'Petter Rasmussen',
	contact		=>	'nil',
	name		=>	'Telegraph',
	description	=>	'Posts notifications to a telegraph server when you receive a pm or is highlighted',
	license		=>	'BSD',
	url			=>	'https://github.com/prasmussen/telegraph',
);

sub telegraph_help {
	Irssi::print('%G>>%n Telegraph can be configured with these settings:');
	Irssi::print('%G>>%n telegraph_send_privmsg : Notify on pm\'s');
	Irssi::print('%G>>%n telegraph_send_highlight : Notify on hilight\'s.');
	Irssi::print('%G>>%n telegraph_host : Telegraph server host');
	Irssi::print('%G>>%n telegraph_port : Telegraph server port');
	Irssi::print('%G>>%n telegraph_message : Message to send on pm/highlight');
}

sub sig_message_private ($$$$) {
	#my ($server, $data, $nick, $address) = @_;
	return unless Irssi::settings_get_bool('telegraph_send_privmsg');
    post_notification();
}

sub sig_print_text ($$$) {
	my ($dest, $text, $stripped) = @_;
	return unless Irssi::settings_get_bool('telegraph_send_highlight');
	
	if ($dest->{level} & MSGLEVEL_HILIGHT) {
        post_notification();
	}
}

sub post_notification() {
    my $ua = LWP::UserAgent->new();
    my $ip = Irssi::settings_get_str('telegraph_host');
    my $message = Irssi::settings_get_str('telegraph_message');
    my $port = Irssi::settings_get_str('telegraph_port');
    my $url = sprintf("http://%s:%s/transmission", $ip, $port);
    $ua->request(POST $url, Content_Type => 'text/plain', Content => $message);
}

Irssi::settings_add_bool($IRSSI{'name'}, 'telegraph_send_privmsg', 1);
Irssi::settings_add_bool($IRSSI{'name'}, 'telegraph_send_highlight', 1);
Irssi::settings_add_str($IRSSI{'name'}, 'telegraph_host', 'localhost');
Irssi::settings_add_str($IRSSI{'name'}, 'telegraph_port', '9090');
Irssi::settings_add_str($IRSSI{'name'}, 'telegraph_message', 'irssi');
Irssi::command_bind('telegraph', 'telegraph_help');
Irssi::signal_add_last('message private', \&sig_message_private);
Irssi::signal_add_last('print text', \&sig_print_text);
Irssi::print('%G>>%n ' . $IRSSI{name} . ' ' . $VERSION . ' loaded (/telegraph for help)');
